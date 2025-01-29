using System;
using UnityEngine;
using Microsoft.CognitiveServices.Speech;

public class SpeechRecognition : MonoBehaviour
{
    private SpeechRecognizer recognizer;
    public CorgiScript corgiScript;

    void Start()
    {
        InitializeSpeechRecognizer();

        if (corgiScript == null)
        {
            corgiScript = GameObject.FindGameObjectWithTag("corgiTag").GetComponent<CorgiScript>();
            if (corgiScript == null)
            {
                Debug.LogError("CorgiScript component not found on GameObject with tag 'corgiTag'.");
            }
        }

    }

    private async void InitializeSpeechRecognizer()
    {
        var config = SpeechConfig.FromSubscription("e028de1ba53a4cb8a9cf3db2ea2acc9e", "germanywestcentral");

        recognizer = new SpeechRecognizer(config);

        recognizer.Recognizing += (s, e) =>
        {
            Debug.Log($"Recognizing: {e.Result.Text}");
        };

        recognizer.Recognized += (s, e) =>
        {
            if (e.Result.Reason == ResultReason.RecognizedSpeech)
            {
                Debug.Log($"Recognized: {e.Result.Text}");
                ProcessVoiceCommand(e.Result.Text);
            }
            else if (e.Result.Reason == ResultReason.NoMatch)
            {
                Debug.Log("No speech could be recognized.");
            }
        };

        recognizer.Canceled += (s, e) =>
        {
            Debug.LogError($"Canceled: Reason={e.Reason}");

            if (e.Reason == CancellationReason.Error)
            {
                Debug.LogError($"ErrorDetails={e.ErrorDetails}");
            }
        };

        recognizer.SessionStarted += (s, e) =>
        {
            Debug.Log("Session started.");
        };

        recognizer.SessionStopped += (s, e) =>
        {
            Debug.Log("Session stopped.");
        };

        await recognizer.StartContinuousRecognitionAsync();
    }

    private void ProcessVoiceCommand(string command)
    {
        Debug.Log($"Processing command: {command}");
        if (command.Contains("si", StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log("Sit voice command recognized");
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                corgiScript.TriggerSit();
            });
        }
        
        if (command.Contains("eat", StringComparison.OrdinalIgnoreCase))
        {
            string foodName = command.Substring(command.IndexOf("eat") + 4).Trim();
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                corgiScript.VoiceCommandEat(foodName);
            });
        }
    
    }
}
