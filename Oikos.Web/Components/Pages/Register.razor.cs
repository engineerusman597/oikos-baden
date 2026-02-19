using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using MudBlazor;
using Oikos.Application.Services.Registration;
using Oikos.Application.Services.Registration.Models;
using Oikos.Application.Services.Authentication.Models;
using Oikos.Application.Services.Subscription;
using Oikos.Application.Services.Subscription.Models;
using Oikos.Application.Services.Stripe;
using Oikos.Common.Resources;
using Oikos.Domain.Constants;
using Stripe;
using Stripe.Checkout;
using System.ComponentModel.DataAnnotations;

namespace Oikos.Web.Components.Pages;

public partial class Register
{
    private int _step = 1;
    private bool _questionnaireDone = false;
    private bool _isLoading = false;
    private RegisterModel _model = new();

    // Plan selection state (step 3)
    private int? _registeredUserId;
    private IReadOnlyList<SubscriptionPlanSummary> _plans = [];
    private int? _selectedPlanId;
    private SubscriptionPlanSummary? _selectedPlan;
    private string _billingInterval = "monthly";
    private bool _isActivatingPlan = false;

    [Inject] private IRegistrationService RegistrationService { get; set; } = null!;
    [Inject] private ISubscriptionPlanService SubscriptionPlanService { get; set; } = null!;
    [Inject] private IOptionsSnapshot<StripeOptions> StripeOptionsSnapshot { get; set; } = null!;

    private readonly Dictionary<string, string> _questions = new()
    {
        ["looking_for_help"]   = "Sind Sie auf der Suche nach einer Kanzlei, die Sie bei Zahlungsausfällen unterstützt?",
        ["dont_know_how"]      = "Wissen Sie oft nicht weiter, wie Sie an Ihr Geld kommen?",
        ["multiple_reminders"] = "Mahnen Sie Ihre Kunden mehrfach an, ohne dass dabei etwas herauskommt?",
        ["time_consuming"]     = "Kostet Sie das Mahnwesen zu viel Zeit und Nerven?",
        ["unpaid_invoices"]    = "Haben Sie unbezahlte Rechnungen, die schon länger offen sind?",
        ["credit_check"]       = "Würden Sie gerne innerhalb von Sekunden die Bonität und Liquidität Ihres Kunden wissen?"
    };

    private readonly Dictionary<string, bool?> _answers = new()
    {
        ["looking_for_help"]   = null,
        ["dont_know_how"]      = null,
        ["multiple_reminders"] = null,
        ["time_consuming"]     = null,
        ["unpaid_invoices"]    = null,
        ["credit_check"]       = null
    };

    private bool AllAnswered => _answers.Values.All(v => v.HasValue);

    protected override void OnInitialized()
    {
        base.OnInitialized();

        var uri = new Uri(_navManager.Uri);
        var query = QueryHelpers.ParseQuery(uri.Query);

        if (query.TryGetValue("partner", out var partnerValues) || query.TryGetValue("partnerCode", out partnerValues))
        {
            var code = partnerValues.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(code))
                _model.PartnerCode = code;
        }

        if (query.TryGetValue("email", out var emailValues))
        {
            var email = emailValues.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(email))
                _model.Email = email;
        }
    }

    private void SetAnswer(string key, bool value)
    {
        _answers[key] = value;
        StateHasChanged();
    }

    private void FinishQuestionnaire()
    {
        _questionnaireDone = true;
    }

    private async Task SubmitRegistration()
    {
        _isLoading = true;
        StateHasChanged();
        await Task.Yield();

        try
        {
            var request = new RegisterUserRequest
            {
                Email = _model.Email ?? string.Empty,
                Password = _model.Password ?? string.Empty,
                Company = _model.Company,
                Gender = _model.Gender,
                Title = _model.Title,
                FirstName = _model.FirstName,
                LastName = _model.LastName,
                PartnerCode = _model.PartnerCode,
                AcceptedPrivacy = _model.AcceptedPrivacy,
                SkipSubscriptionCheck = true
            };

            var result = await RegistrationService.RegisterUserAsync(request);

            if (result.Success)
            {
                try
                {
                    var loginRequest = new LoginRequest
                    {
                        Identifier = result.UserName ?? _model.Email!,
                        Password = result.Password ?? _model.Password!,
                        IpAddress = _httpAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString(),
                        UserAgent = _httpAccessor.HttpContext?.Request?.Headers["User-Agent"].ToString()
                    };

                    var loginResult = await _authenticationService.LoginAsync(loginRequest);
                    if (loginResult.Success)
                    {
                        var cookieUtil = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/cookieUtil.js");
                        await cookieUtil.InvokeVoidAsync("setCookie", CommonConstant.UserToken, loginResult.Token);
                        _authService.SetCurrentUser(loginResult.Token!);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Auto login failed: {ex.Message}");
                }

                // Store user id and advance to plan selection
                _registeredUserId = result.UserId;
                _plans = await SubscriptionPlanService.GetPlansAsync();
                _step = 3;
            }
            else
            {
                _snackbarService.Add(result.ErrorMessage ?? Loc["Register_Error"], Severity.Error);
            }
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    // Step 3: create Stripe Checkout Session and redirect to hosted payment page
    private async Task ActivateAndProceed()
    {
        if (_selectedPlanId == null) return;
        _selectedPlan = _plans.FirstOrDefault(p => p.Id == _selectedPlanId);
        if (_selectedPlan == null) return;

        _isActivatingPlan = true;
        StateHasChanged();

        try
        {
            var stripeOptions = StripeOptionsSnapshot.Get("StripeTest");
            var price = _billingInterval == "yearly" ? _selectedPlan.YearlyPrice : _selectedPlan.MonthlyPrice;
            var interval = _billingInterval == "yearly" ? "year" : "month";

            var baseUrl = _navManager.BaseUri.TrimEnd('/');
            var successUrl = $"{baseUrl}/register/success?userId={_registeredUserId}&planId={_selectedPlanId}&billingInterval={_billingInterval}&session_id={{CHECKOUT_SESSION_ID}}";
            var cancelUrl = $"{baseUrl}/register";

            var sessionOptions = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(price * 100),
                            Currency = "eur",
                            Recurring = new SessionLineItemPriceDataRecurringOptions
                            {
                                Interval = interval,
                            },
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = _selectedPlan.Name,
                                Description = _selectedPlan.Description ?? _selectedPlan.Name,
                            },
                        },
                        Quantity = 1,
                    },
                },
                Mode = "subscription",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                CustomerEmail = _model.Email,
                Locale = "de",
            };

            var service = new SessionService(new StripeClient(stripeOptions.ApiKey));
            var session = await service.CreateAsync(sessionOptions);

            _navManager.NavigateTo(session.Url, forceLoad: true);
        }
        catch (StripeException ex)
        {
            _snackbarService.Add($"Stripe Fehler: {ex.StripeError?.Message ?? ex.Message}", Severity.Error);
        }
        catch (Exception ex)
        {
            _snackbarService.Add(ex.Message, Severity.Error);
        }
        finally
        {
            _isActivatingPlan = false;
            StateHasChanged();
        }
    }

    private void SkipPlanSelection()
    {
        _snackbarService.Add(Loc["Register_Success"], Severity.Success);
        _navManager.NavigateTo("/");
    }

    private class RegisterModel
    {
        [Required(ErrorMessageResourceType = typeof(SharedResource), ErrorMessageResourceName = "Register_CompanyRequired")]
        public string? Company { get; set; }

        [Required(ErrorMessageResourceType = typeof(SharedResource), ErrorMessageResourceName = "Register_GenderRequired")]
        public string? Gender { get; set; }

        public string? Title { get; set; }

        [Required(ErrorMessageResourceType = typeof(SharedResource), ErrorMessageResourceName = "Register_FirstNameRequired")]
        public string? FirstName { get; set; }

        [Required(ErrorMessageResourceType = typeof(SharedResource), ErrorMessageResourceName = "Register_LastNameRequired")]
        public string? LastName { get; set; }

        [Required(ErrorMessageResourceType = typeof(SharedResource), ErrorMessageResourceName = "Register_EmailRequired")]
        [EmailAddress(ErrorMessageResourceType = typeof(SharedResource), ErrorMessageResourceName = "Register_EmailInvalid")]
        public string? Email { get; set; }

        [Required(ErrorMessageResourceType = typeof(SharedResource), ErrorMessageResourceName = "Register_PasswordRequired")]
        [MinLength(6, ErrorMessageResourceType = typeof(SharedResource), ErrorMessageResourceName = "Register_PasswordMinLength")]
        public string? Password { get; set; }

        [Required(ErrorMessageResourceType = typeof(SharedResource), ErrorMessageResourceName = "Register_ConfirmPasswordRequired")]
        [Compare(nameof(Password), ErrorMessageResourceType = typeof(SharedResource), ErrorMessageResourceName = "Register_PasswordMismatch")]
        public string? ConfirmPassword { get; set; }

        public bool AcceptedPrivacy { get; set; }

        [MaxLength(50)]
        public string? PartnerCode { get; set; }
    }
}
