using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure.Communication;
using Azure.Communication.CallAutomation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ACSFunctions
{
    public class BlobToACS
    {
        private readonly ILogger<BlobToACS> _logger;

        public BlobToACS(ILogger<BlobToACS> logger)
        {
            _logger = logger;
        }

        [Function(nameof(BlobToACS))]
        public async Task Run([BlobTrigger("tickets/{name}", Connection = "AzureWebJobsStorage")] Stream stream, string name, Uri uri, IDictionary<string, string> metaData)
        {
            using var blobStreamReader = new StreamReader(stream);
            var content = await blobStreamReader.ReadToEndAsync();
            _logger.LogInformation($"C# Blob trigger function Processed blob\n Name: {name} ");
            _logger.LogInformation(uri.ToString());
            // get full URL of blob
            // genereate a sas key for the blob
            // pass the sas key to the call automation client
            // call the call automation client

            // get the the phone number from the metadata

            var acsPhonenumber = Environment.GetEnvironmentVariable("acsPhonenumber");
            ArgumentNullException.ThrowIfNullOrEmpty(acsPhonenumber);
            var targetPhonenumber = Environment.GetEnvironmentVariable("targetPhonenumber");
            ArgumentNullException.ThrowIfNullOrEmpty(targetPhonenumber);
            string key = "PhoneNumber";
            if (metaData.TryGetValue(key, out string value))
            {
                targetPhonenumber = value;
                _logger.LogInformation($"Value for '{key}': {targetPhonenumber}");
            }
            else
            {
                _logger.LogWarning($"Key '{key}' not found in metadata.");
            }


          //  var metadata = metaData.Select(x => $"{x.Key}: {x.Value}");

            var acsConnectionString = Environment.GetEnvironmentVariable("AcsConnectionString");
            ArgumentNullException.ThrowIfNullOrEmpty(acsConnectionString);

          

            // Target phone number you want to receive the call.
         

            var callbackUriHost = Environment.GetEnvironmentVariable("callbackUriHost");
            ArgumentNullException.ThrowIfNullOrEmpty(callbackUriHost);

            _logger.LogInformation($"targetPhonenumber: {targetPhonenumber}");
            _logger.LogInformation(acsConnectionString);
            _logger.LogInformation($"acsphonumner: {acsPhonenumber}");
            _logger.LogInformation(callbackUriHost);

            var callbackUri = new Uri(new Uri(callbackUriHost), "/api/callback");

            CallInvite callInvite = new CallInvite(
        new PhoneNumberIdentifier(targetPhonenumber),
        new PhoneNumberIdentifier(acsPhonenumber)
    );
            string[] audioFiles = {
            "BabyElephantWalk60.wav",
            "CantinaBand3.wav",
            "CantinaBand60.wav",
            "Fanfare60.wav",
            "gettysburg10.wav",
            "gettysburg.wav",
            "ImperialMarch60.wav",
            "PinkPanther30.wav",
            "PinkPanther60.wav",
            "preamble10.wav",
            "preamble.wav",
            "StarWars3.wav",
            "StarWars60.wav",
            "taunt.wav"
        };
            Random random = new Random();
            int index = random.Next(audioFiles.Length);
            string selectedFile = "https://www2.cs.uic.edu/~i101/SoundFiles/" + audioFiles[index];
           
            _logger.LogInformation(selectedFile);



            CallAutomationClient callAutomationClient = new CallAutomationClient(acsConnectionString);
            var createCallOptions = new CreateCallOptions(callInvite, callbackUri)
            {
                OperationContext = selectedFile
            };


            var createCallResult = await callAutomationClient.CreateCallAsync(createCallOptions);


            _logger.LogWarning("call connecting state");
            _logger.LogWarning(createCallResult.Value.CallConnectionProperties.CallConnectionState.ToString());

        }
    }
}
