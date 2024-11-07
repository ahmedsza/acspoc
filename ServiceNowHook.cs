using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Logging;
using System.Drawing;
using System.IO;
using System.Text.Json;

namespace ACSFunctions
{

    public class MultiOutput
    {
        //    [HttpResult]
         public HttpResponseData Result { get; set; }

        [BlobOutput("tickets/{rand-guid}.mp3")]
        public byte[] ticket { get; set; }
    }
    public class Ticket
    {
        public string? Description { get; set; }
        public string? Date { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Recipient { get; set; }
    }



    public class ServiceNowHook
    {
        private readonly ILogger<ServiceNowHook> _logger;

        public ServiceNowHook(ILogger<ServiceNowHook> logger)
        {
            _logger = logger;
        }







        static async Task<byte[]> SynthesizeAudioAsync(string text, string subscriptionKey, string region)
        {
            var speechConfig = SpeechConfig.FromSubscription(subscriptionKey, region);
            speechConfig.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Audio16Khz32KBitRateMonoMp3);

            using var speechSynthesizer = new SpeechSynthesizer(speechConfig, null);

            var result = await speechSynthesizer.SpeakTextAsync(text);
            using var stream = AudioDataStream.FromResult(result);

            using var memoryStream = new MemoryStream();
            byte[] buffer = new byte[1024];
            uint bytesRead;

            while ((bytesRead = stream.ReadData(buffer)) > 0)
            {
                memoryStream.Write(buffer, 0, (int)bytesRead);
            }

            return memoryStream.ToArray();
        }






        [Function("ServiceNowHook")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            // Read the data via a post call and put into the Ticket object
            var ticket = await JsonSerializer.DeserializeAsync<Ticket>(req.Body);

            // text to speech

            var speechKey = Environment.GetEnvironmentVariable("speechKey");
            ArgumentNullException.ThrowIfNullOrEmpty(speechKey);


            var speechRegion = Environment.GetEnvironmentVariable("speechRegion");
            ArgumentNullException.ThrowIfNullOrEmpty(speechRegion);
            var outputstream = await SynthesizeAudioAsync(ticket.Description, speechKey, speechRegion);

            // convert outputstream to stream
             var stream = new MemoryStream(outputstream);

            // if you want directly write to azure storage..uncomment below. 

            var storageAccount = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            ArgumentNullException.ThrowIfNullOrEmpty(storageAccount);
            var blobServiceClient = new Azure.Storage.Blobs.BlobServiceClient(storageAccount);
            var containerClient = blobServiceClient.GetBlobContainerClient("tickets");
            // create unique ticket name with mp3 extension

            var ticketName = Guid.NewGuid().ToString() + ".mp3";
            var blobClient = containerClient.GetBlobClient(ticketName);
            // set some metadata on the blobclient
  

            var metadata = new Dictionary<string, string>
{
    { "Description", ticket.Description },
    { "Date", ticket.Date },
    { "PhoneNumber", ticket.PhoneNumber },
    { "Recipient", ticket.Recipient }
};

            // Set the metadata on the blob
            await blobClient.UploadAsync(stream, true);
            await blobClient.SetMetadataAsync(metadata);

       //     await blobClient.UploadAsync(stream);


            //// Create the response
            //var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            //await response.WriteAsJsonAsync(ticket);
            //// convert outputstream to byte array


            //var myObject = new MultiOutput
            //{
            //  Result = response,
            // //  ticket =  stream.ToArray()
            //};
            //return myObject;
            return new OkObjectResult("Welcome to Azure Functions!");

        }





    }
}

