using System.Threading.Tasks;
using MediatR;
using NServiceBus;
using NServiceBus.Logging;

#region SuccessHandler

public class SuccessHandler :
    IHandleMessages<MessageThatWillSucceed>
{
    public SuccessHandler(IMediator mediator, ITest test)
    {
        _mediator = mediator;
        _test = test;
    }

    static ILog log = LogManager.GetLogger<SuccessHandler>();
    private IMediator _mediator;
    private ITest _test;

    public async Task Handle(MessageThatWillSucceed message, IMessageHandlerContext context)
    {
        await _mediator.Publish(new Meow { }).ConfigureAwait(false);

        log.Info("Received a MessageThatWillSucceed");
    }
}

public class Meow : INotification
{
}

#endregion