using System.Threading.Channels;

namespace Oikos.Common.Helpers;

public class ChannelHelper<TModel>
{
    private static readonly Lazy<ChannelHelper<TModel>> lazy =
        new(() => new ChannelHelper<TModel>());

    private readonly ChannelWriter<TModel> _writer;
    private readonly ChannelReader<TModel> _reader;

    static ChannelHelper()
    {
    }

    private ChannelHelper()
    {
        var channelOptions = new BoundedChannelOptions(1000)
        {
            // Behavior when the channel is full.
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleWriter = false
        };
        var channel = Channel.CreateBounded<TModel>(channelOptions);
        _writer = channel.Writer;
        _reader = channel.Reader;
    }

    public static ChannelHelper<TModel> Instance => lazy.Value;

    public ChannelWriter<TModel> Writer => _writer;

    public ChannelReader<TModel> Reader => _reader;
}
