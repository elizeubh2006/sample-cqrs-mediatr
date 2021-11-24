using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sample.MediatR.Application.Notifications.ProductSavedNotification;
using Sample.MediatR.Application.Notifications.SendEmailNotification;

namespace Sample.MediatR.Application.Commands.ProductSaveCommand;

public class ProductSaveCommandHandler : IRequestHandler<ProductSaveCommand, string>
{
    private readonly IMediator _mediator;

    public ProductSaveCommandHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<string> Handle(ProductSaveCommand request, CancellationToken cancellationToken)
    {
        //insert product in database
        Serilog.Log.Information($"Product saved successfully. Id: {request.Id}");

        await _mediator.Publish(new ProductSavedNotification { Id = request.Id }, cancellationToken);
        await _mediator.Publish(new SendEmailNotification { Email = "test@mail.com" }, cancellationToken);

        return await Task.FromResult("Ok");
    }
}
