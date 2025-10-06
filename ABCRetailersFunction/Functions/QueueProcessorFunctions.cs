using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABCRetailersFunction.Functions
{
    public class QueueProcessorFunctions
    {
     
            [Function("OrderNotifications_Processor")]
            public void OrderNotificationsProcessor(
            [QueueTrigger("%QUEUE_ORDER_NOTIFICATIONS%", Connection = "STORAGE_CONNECTION")] string message,
            FunctionContext ctx)
            {
                var log = ctx.GetLogger("OrderNotifications_Processor");

                // TEMPORARY: Throw exception to keep message in queue for screenshot
                log.LogInformation($"📦 ORDER QUEUE MESSAGE CAPTURED: {message}");
                throw new Exception("TEMPORARY: Keeping message in queue for screenshot");

                /*
                // NORMAL CODE (commented out):
                log.LogInformation($"OrderNotifications message: {message}");
                */
            }

            [Function("StockUpdates_Processor")]
            public void StockUpdatesProcessor(
                [QueueTrigger("%QUEUE_STOCK_UPDATES%", Connection = "STORAGE_CONNECTION")] string message,
                FunctionContext ctx)
            {
                var log = ctx.GetLogger("StockUpdates_Processor");

                // TEMPORARY: Throw exception to keep message in queue for screenshot
                log.LogInformation($"📊 STOCK QUEUE MESSAGE CAPTURED: {message}");
                throw new Exception("TEMPORARY: Keeping message in queue for screenshot");

                /*
                // NORMAL CODE (commented out):
                log.LogInformation($"StockUpdates message: {message}");
                */
            }
        }
    }

