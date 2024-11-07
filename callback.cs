using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Xml.Linq;
using System.Net.Http;
using System.Diagnostics.Eventing.Reader;

namespace ACSFunctions
{
    public class callback
    {
        private readonly ILogger<callback> _logger;

        public callback(ILogger<callback> logger)
        {
            _logger = logger;
        }

        [Function("callback")]
        public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            var acsConnectionString = Environment.GetEnvironmentVariable("AcsConnectionString");
            ArgumentNullException.ThrowIfNullOrEmpty(acsConnectionString);
            CallAutomationClient callAutomationClient = new CallAutomationClient(acsConnectionString);

            // get the http content
            using (var memoryStream = new MemoryStream())
            {
                await req.Body.CopyToAsync(memoryStream);
                var bytes = memoryStream.ToArray();
                // Parse the JSON payload into a list of events
                CloudEvent[] cloudEvents = CloudEvent.ParseMany(new BinaryData(bytes));

                foreach (var cloudEvent in cloudEvents)
                {
                    _logger.LogInformation("Received call event: {type}, callConnectionID: {connId}, serverCallId: {serverId}",
                        cloudEvent.Type,
                        cloudEvent.Subject,
                        cloudEvent.Id);
                    CallAutomationEventBase parsedEvent = CallAutomationEventParser.Parse(cloudEvent);
                    _logger.LogInformation(
                                    "Received call event: {type}, callConnectionID: {connId}, serverCallId: {serverId}",
                                    parsedEvent.GetType(),
                                    parsedEvent.CallConnectionId,
                                    parsedEvent.ServerCallId);
                    if (parsedEvent is CallConnected callConnected)
                    {
                        var x = callAutomationClient.GetCallConnection(parsedEvent.CallConnectionId);
                        var z = x.GetParticipants();
                        var participantsResponse = x.GetParticipants();
                        var participants = participantsResponse.Value;
                        string targetPhonenumber="";
                        foreach (var item in participants)
                   
                        {
                            var identifier = item.Identifier.ToString();
                            _logger.LogInformation(identifier);
                            if (identifier.Contains("acs"))
                            {
                                _logger.LogInformation("found acs");
                            }
                            else
                            {
                                _logger.LogInformation("not found acs");
                                targetPhonenumber = identifier;
                            }
                            _logger.LogInformation(item.ToString());
                        //    _logger.LogInformation(item.Identifier.ToString());
                          



                        }
                        

                        _logger.LogInformation(callConnected.OperationContext);
                        _logger.LogInformation("in call connected...");
                        string mp3Url = callConnected.OperationContext;
                       // string mp3Url = "https://www2.cs.uic.edu/~i101/SoundFiles/StarWars3.wav";
                        var playSource = new FileSource(new Uri(mp3Url));
                        //var targetPhonenumber = Environment.GetEnvironmentVariable("targetPhonenumber");
                        //ArgumentNullException.ThrowIfNullOrEmpty(targetPhonenumber);
                        PhoneNumberIdentifier target = new PhoneNumberIdentifier(targetPhonenumber);
                        //   var playTo = new List<CommunicationIdentifier> { target };
                        //  var playResponse = await callAutomationClient.GetCallConnection(parsedEvent.CallConnectionId)
                        //      .GetCallMedia()
                        //      .PlayAsync(playSource, playTo);

    //                    var playResponse = await callAutomationClient.GetCallConnection(parsedEvent.CallConnectionId)
    //.GetCallMedia()
    //.PlayToAllAsync(playSource);
                        var maxTonesToCollect = 3;
                        //    String textToPlay = "Welcome to Contoso, please enter 3 DTMF.";
                        //   var playSource = new TextSource(textToPlay, "en-US-ElizabethNeural");
                        var recognizeOptions = new CallMediaRecognizeDtmfOptions(target, maxTonesToCollect)
                        {
                            InitialSilenceTimeout = TimeSpan.FromSeconds(30),
                            Prompt = playSource,
                            InterToneTimeout = TimeSpan.FromSeconds(5),
                            InterruptPrompt = true,
                            StopTones = new DtmfTone[] {
      DtmfTone.Pound
    },
                            
                        };
                        var recognizeResult = await callAutomationClient.GetCallConnection(parsedEvent.CallConnectionId)
                          .GetCallMedia()
                          .StartRecognizingAsync(recognizeOptions);




                    }
                    else if (parsedEvent is RecognizeCompleted recognizeCompleted)
                    {
                        _logger.LogInformation("in recognize completed...");
                        switch (recognizeCompleted.RecognizeResult)
                        {
                            case DtmfResult dtmfResult:
                                //Take action for Recognition through DTMF 
                                var tones = dtmfResult.Tones;
                                _logger.LogInformation("Recognize completed succesfully, tones={tones}", tones);
                                break;
                            case ChoiceResult choiceResult:
                                // Take action for Recognition through Choices 
                                var labelDetected = choiceResult.Label;
                                var phraseDetected = choiceResult.RecognizedPhrase;
                                // If choice is detected by phrase, choiceResult.RecognizedPhrase will have the phrase detected, 
                                // If choice is detected using dtmf tone, phrase will be null 
                                _logger.LogInformation("Recognize completed succesfully, labelDetected={labelDetected}, phraseDetected={phraseDetected}", labelDetected, phraseDetected);
                                break;
                            case SpeechResult speechResult:
                                // Take action for Recognition through Choices 
                                var text = speechResult.Speech;
                                _logger.LogInformation("Recognize completed succesfully, text={text}", text);
                                break;
                            default:
                                _logger.LogInformation("Recognize completed succesfully, recognizeResult={recognizeResult}", recognizeCompleted.RecognizeResult);
                                break;
                        }
                        
                    }
                    else if (parsedEvent is RecognizeFailed recognizeFailed)
                    {
                        if (MediaEventReasonCode.RecognizeInitialSilenceTimedOut.Equals(recognizeFailed.ReasonCode))
                        {
                            // Take action for time out 
                            _logger.LogInformation("Recognition failed: initial silencev time out");
                        }
                        else if (MediaEventReasonCode.RecognizeSpeechOptionNotMatched.Equals(recognizeFailed.ReasonCode))
                        {
                            // Take action for option not matched 
                            _logger.LogInformation("Recognition failed: speech option not matched");
                        }
                        else if (MediaEventReasonCode.RecognizeIncorrectToneDetected.Equals(recognizeFailed.ReasonCode))
                        {
                            // Take action for incorrect tone 
                            _logger.LogInformation("Recognition failed: incorrect tone detected");
                        }
                        else
                        {
                            _logger.LogInformation("Recognition failed, result={result}, context={context}", recognizeFailed.ResultInformation?.Message, recognizeFailed.OperationContext);
                        }
                    }



                    //    using (var reader = new StreamReader(req.Body))
                    //    {
                    //        var payload = await reader.ReadToEndAsync().ConfigureAwait(false);
                    //        _logger.LogInformation("Received payload: {payload}", payload);
                    //        var cloudEvent = CloudEvent.Parse(payload);
                    //        _logger.LogInformation("Received call event: {type}, callConnectionID: {connId}, serverCallId: {serverId}",
                    //            cloudEvent.Type,
                    //            cloudEvent.Subject,
                    //            cloudEvent.Id);
                    //        CallAutomationEventBase parsedEvent = CallAutomationEventParser.Parse(cloudEvent);
                    //        _logger.LogInformation(
                    //                        "Received call event: {type}, callConnectionID: {connId}, serverCallId: {serverId}",
                    //                        parsedEvent.GetType(),
                    //                        parsedEvent.CallConnectionId,
                    //                        parsedEvent.ServerCallId);
                    //        if (parsedEvent is CallConnected callConnected)
                    //        {
                    //            _logger.LogInformation("in call connected...");
                    //            string mp3Url = "https://www2.cs.uic.edu/~i101/SoundFiles/StarWars3.wav";
                    //            var playSource = new FileSource(new Uri(mp3Url));
                    //            var targetPhonenumber = Environment.GetEnvironmentVariable("targetPhonenumber");
                    //            ArgumentNullException.ThrowIfNullOrEmpty(targetPhonenumber);
                    //            PhoneNumberIdentifier target = new PhoneNumberIdentifier(targetPhonenumber);
                    //            var playTo = new List<CommunicationIdentifier> { target };
                    //            var playResponse = await callAutomationClient.GetCallConnection(parsedEvent.CallConnectionId)
                    //                .GetCallMedia()
                    //                .PlayAsync(playSource, playTo);

                    //        }
                    //    }
                }
                return new OkObjectResult("Welcome to Azure Functions!");
            }
        }
    }
}
