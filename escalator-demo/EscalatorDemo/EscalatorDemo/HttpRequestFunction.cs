using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace EscalatorDemo
{
    public static class HttpRequestFunction
    {
        [FunctionName("HttpRequestFunction")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Drive Gear Temperature Service triggered");

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<EscalatorRequest>(requestBody);

            if (data?.Readings is not null)
            {
                var response = new List<ReadingResult>(data.Readings.Length);

                foreach (var reading in data.Readings)
                {
                    var result = new ReadingResult
                    {
                        Temperature = reading.Temperature,
                        DriveGearId = reading.DriveGearId,
                        TimeStamp = reading.TimeStamp
                    };

                    switch (reading.Temperature)
                    {
                        case <= 25:
                            log.LogInformation($"Drive Gear {reading.DriveGearId} is running at normal temperature");
                            result.Status = "Normal";
                            break;
                        case > 25 and <= 50:
                            log.LogWarning($"Drive Gear {reading.DriveGearId} is running at elevated temperature");
                            result.Status = "Elevated";
                            break;
                        default:
                            log.LogError($"Drive Gear {reading.DriveGearId} is running at critical temperature");
                            result.Status = "Critical";
                            break;
                    }

                    response.Add(result);
                }

                return new OkObjectResult(new ReadingResponse { Readings = response.ToArray() });
            }

            return new BadRequestErrorMessageResult("Please send an array of readings in the request body");
        }
    }

    public class EscalatorRequest
    {
        public Reading[] Readings { get; set; }
    }

    public class Reading
    {
        public int DriveGearId { get; set; }

        public DateTimeOffset TimeStamp { get; set; }

        public int Temperature { get; set; }
    }

    public class ReadingResponse
    {
        public ReadingResult[] Readings { get; set; }
    }

    public class ReadingResult : Reading
    {
        public string Status { get; set; }
    }
}
