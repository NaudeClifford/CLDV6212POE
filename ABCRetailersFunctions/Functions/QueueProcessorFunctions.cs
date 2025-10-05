using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ABCRetailersFunctions.Functions;

public class QueueProcessorFunctions
{

    [Function("OrderNotifications_Processor")]
    public void OrderNotificationsProcessor
        ([QueueTrigger("%QUEUE_ORDER_NOTIFICATIONS%", Connection = "STORAGE_CONNECTION")]

        string message,
        FunctionContext con)
    {
        var log = con.GetLogger("OrderNotifications_Processor");
        log.LogInformation($"OrderNotifications message: {message}");
        //Optional write receipts, send emails, etc.
    }

    [Function("StockUpdates_Processor")]
    public void StockUpdatesProcessor(
        [QueueTrigger("%QUEUE_STOCK_UPDATES%", Connection = "STORAGE_CONNECTION")]
        string message,
        FunctionContext con)
    {
        var log = con.GetLogger("StockUpdates_Processor");
        log.LogInformation($"StockUpdates message: {message}");
        //Optional snyc to reporting DB, etc
    }
}