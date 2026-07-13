/*
using System;
using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class MultimodalAiTest : MonoBehaviour
{
    [Header("Llama Server Settings")]
    [SerializeField] private bool autoStartServer = true;
    [SerializeField] private int serverPort = 13333;
    [SerializeField] private string modelFileName = "gemma-4-E2B_q4_0-it.gguf";
    [SerializeField] private string mmprojFileName = "gemma-4-E2B-it-mmproj.gguf";
    [SerializeField, TextArea(3, 5)] private string systemPrompt = "Always respond in Korean. 친절하게 한국어로만 답변해 주세요.";

    [Header("Generator Settings")]
    [Range(0f, 2f)][SerializeField] private float temperature = 0.7f;
    [Range(0f, 2f)][SerializeField] private float frequencyPenalty = 1.0f;
    [Range(0f, 2f)][SerializeField] private float presencePenalty = 1.0f;
    [Tooltip("Maximum tokens to generate. Set to 0 to use server default.")]
    [SerializeField] private int maxTokens = 100;
    [Tooltip("If checked, the server's default token limit will be used, ignoring the Max Tokens value above.")]
    [SerializeField] private bool useDefaultMaxTokens = false;

    [Header("Rule Engine Settings (llama.cpp Grammar)")]
    [Tooltip("Define a GBNF grammar to restrict the model output format (e.g., json format or specific tags).")]
    [SerializeField, TextArea(3, 5)] private string gbnfGrammar = "";

    [Header("UI Elements")]
    [SerializeField] private TMP_InputField chatInputField;
    [SerializeField] private Button sendButton;
    [SerializeField] private TextMeshProUGUI responseText;
    [SerializeField] private Button captureButton;
    [SerializeField] private RawImage photoDisplay;

    [Header("Confirm Popup UI")]
    [SerializeField] private GameObject confirmPopup;
    [SerializeField] private Button popupYesButton;
    [SerializeField] private Button popupNoButton;

    [Header("Development Settings")]
    [SerializeField] private Texture2D dummyTexture;

    private string serverUrl;
    private WebCamTexture webcamTexture;
    private Texture2D capturedTexture;
    private System.Diagnostics.Process serverProcess;
    private string lastSentPrompt;

    // Serializable structures for parsing JSON Response via JsonUtility
    [Serializable]
    private class LlamaResponse
    {
        public Choice[] choices;
        public UsageInfo usage;

        [Serializable]
        public class Choice
        {
            public Message message;
        }

        [Serializable]
        public class Message
        {
            public string role;
            public string content;
        }

        [Serializable]
        public class UsageInfo
        {
            public int prompt_tokens;
            public int completion_tokens;
            public int total_tokens;
        }
    }

    void Start()
    {
        // Build Server URL based on Port (127.0.0.1 is more stable than localhost on macOS)
        serverUrl = $"http://127.0.0.1:{serverPort}/v1/chat/completions";

        // Initialize UI
        if (confirmPopup != null)
            confirmPopup.SetActive(false);

        if (photoDisplay != null)
            photoDisplay.gameObject.SetActive(false);

        // Bind Button Listeners
        if (captureButton != null)
            captureButton.onClick.AddListener(Capture);

        if (popupYesButton != null)
            popupYesButton.onClick.AddListener(SendImageAndText);

        if (popupNoButton != null)
            popupNoButton.onClick.AddListener(CancelSend);

        if (sendButton != null)
            sendButton.onClick.AddListener(SendImageAndText);

        if (chatInputField != null)
            chatInputField.onSubmit.AddListener(OnChatInputSubmit);

        // Start Llama Server process inside Unity
        StartLlamaServer();

        // Initialize Camera
        StartCoroutine(InitCameraRoutine());
    }

    private IEnumerator InitCameraRoutine()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // Request Camera Permission on Android
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            Permission.RequestUserPermission(Permission.Camera);
            yield return new WaitUntil(() => Permission.HasUserAuthorizedPermission(Permission.Camera));
        }

        // Initialize and Play WebCamTexture
        if (WebCamTexture.devices.Length > 0)
        {
            webcamTexture = new WebCamTexture(WebCamTexture.devices[0].name, 1280, 720, 30);
            if (photoDisplay != null)
            {
                photoDisplay.texture = webcamTexture;
                photoDisplay.gameObject.SetActive(true);
            }
            webcamTexture.Play();
        }
        else
        {
            Debug.LogError("No camera device found on Android.");
        }
#else
        // Set Dummy Image for Mac/Editor Development Environment
        if (dummyTexture != null && photoDisplay != null)
        {
            photoDisplay.texture = dummyTexture;
            Debug.Log("[Editor] Dummy image set to photoDisplay.");
        }
#endif
        yield return null;
    }

    private void StartLlamaServer()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        if (!autoStartServer) return;

        // Clean up any orphaned llama-server processes from previous crashes or compiles
        KillExistingLlamaProcesses();

        string binaryFolder = Path.Combine(Application.streamingAssetsPath, "LlamaServer");
        string binaryName = "llama-server";
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        binaryName = "llama-server.exe";
#endif
        string binaryPath = Path.Combine(binaryFolder, binaryName);

        if (!File.Exists(binaryPath))
        {
            Debug.LogWarning($"[LlamaServer] Executable not found at: {binaryPath}. Automatically launching is skipped. Please launch manually.");
            return;
        }

        string modelPath = Path.Combine(binaryFolder, modelFileName);
        string mmprojPath = Path.Combine(binaryFolder, mmprojFileName);

        if (!File.Exists(modelPath) || !File.Exists(mmprojPath))
        {
            Debug.LogWarning($"[LlamaServer] Model files missing. Ensure {modelFileName} and {mmprojFileName} exist in {binaryFolder}");
            return;
        }

        string arguments = $"-m \"{modelPath}\" --mmproj \"{mmprojPath}\" -c 2048 --port {serverPort}";

        try
        {
            serverProcess = new System.Diagnostics.Process();
            serverProcess.StartInfo.FileName = binaryPath;
            serverProcess.StartInfo.Arguments = arguments;
            serverProcess.StartInfo.UseShellExecute = false;
            serverProcess.StartInfo.CreateNoWindow = true; // Run in background without window popup
            serverProcess.StartInfo.RedirectStandardOutput = true;
            serverProcess.StartInfo.RedirectStandardError = true;

            // Route standard output and error to Unity Console for error tracking
            serverProcess.OutputDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                    Debug.Log($"[LlamaServer Log] {args.Data}");
            };
            serverProcess.ErrorDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    // Route to appropriate log severity based on content keywords
                    if (args.Data.Contains("error") || args.Data.Contains("ERR") || args.Data.Contains("failed") || args.Data.Contains("FAIL"))
                    {
                        Debug.LogError($"[LlamaServer Err] {args.Data}");
                    }
                    else if (args.Data.Contains("warning") || args.Data.Contains("WRN") || args.Data.Contains("bug in the model"))
                    {
                        Debug.LogWarning($"[LlamaServer Wrn] {args.Data}");
                    }
                    else
                    {
                        // llama-server sends all routine startup updates/info to stderr. We route them to normal Debug.Log.
                        Debug.Log($"[LlamaServer Log] {args.Data}");
                    }
                }
            };

            serverProcess.Start();
            serverProcess.BeginOutputReadLine();
            serverProcess.BeginErrorReadLine();

            Debug.Log($"[LlamaServer] Server started automatically inside Unity. PID: {serverProcess.Id}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LlamaServer] Failed to start server process: {ex.Message}");
        }
#endif
    }

    private void StopLlamaServer()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        if (serverProcess != null && !serverProcess.HasExited)
        {
            try
            {
                serverProcess.Kill();
                serverProcess.Dispose();
                Debug.Log("[LlamaServer] Server process killed successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LlamaServer] Error killing server process: {ex.Message}");
            }
            serverProcess = null;
        }
#endif
    }

    private void KillExistingLlamaProcesses()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        try
        {
            // Find all running processes named 'llama-server'
            var processes = System.Diagnostics.Process.GetProcessesByName("llama-server");
            foreach (var p in processes)
            {
                try
                {
                    p.Kill();
                    p.WaitForExit(1000);
                    Debug.Log($"[LlamaServer] Cleaned up existing/zombie server process (PID: {p.Id})");
                }
                catch (Exception)
                {
                    // Ignore exceptions for processes that are already exiting
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[LlamaServer] Info during process cleanup: {ex.Message}");
        }
#endif
    }

    private void Capture()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (webcamTexture != null && webcamTexture.isPlaying)
        {
            // Copy WebCamTexture frame to Texture2D
            capturedTexture = new Texture2D(webcamTexture.width, webcamTexture.height, TextureFormat.RGBA32, false);
            capturedTexture.SetPixels(webcamTexture.GetPixels());
            capturedTexture.Apply();

            // Display static captured image
            if (photoDisplay != null)
            {
                photoDisplay.texture = capturedTexture;
                photoDisplay.gameObject.SetActive(true);
            }
            
            // Pause live stream
            webcamTexture.Pause();
        }
        else
        {
            Debug.LogWarning("Webcam is not active or playing.");
        }
#else
        // Capture Dummy Image in Editor
        if (dummyTexture != null)
        {
            capturedTexture = dummyTexture;
            if (photoDisplay != null)
            {
                photoDisplay.texture = capturedTexture;
                photoDisplay.gameObject.SetActive(true);
            }
            Debug.Log("[Editor] Dummy image captured.");
        }
        else
        {
            Debug.LogError("[Editor] Dummy Texture is not assigned in the Inspector.");
        }
#endif

        // Show Confirm Popup
        if (confirmPopup != null)
        {
            confirmPopup.SetActive(true);
        }
    }

    private void SendImageAndText()
    {
        if (string.IsNullOrEmpty(serverUrl))
        {
            Debug.LogError("Server URL is not assigned.");
            return;
        }

        // Escape input text and system prompt for JSON compatibility (handling newlines as well)
        string userText = chatInputField != null ? chatInputField.text : "";
        lastSentPrompt = userText; // Store last sent question for debugging
        string escapedText = userText.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        string escapedSystemPrompt = systemPrompt.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");

        // Get culture-invariant string representations of float parameters
        string tempStr = temperature.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string freqStr = frequencyPenalty.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string presStr = presencePenalty.ToString(System.Globalization.CultureInfo.InvariantCulture);

        // Prepare grammar field if specified
        string grammarJsonField = "";
        if (!string.IsNullOrEmpty(gbnfGrammar))
        {
            string escapedGrammar = gbnfGrammar.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");
            grammarJsonField = $",\"grammar\":\"{escapedGrammar}\"";
        }

        // Prepare max_tokens field if specified and not using default
        string maxTokensJsonField = "";
        if (!useDefaultMaxTokens && maxTokens > 0)
        {
            maxTokensJsonField = $",\"max_tokens\":{maxTokens}";
        }

        string jsonPayload = "";
        string statusText = "";

        if (capturedTexture != null)
        {
            // Encode texture to PNG and convert to Base64 (Vision Mode)
            byte[] imageBytes = capturedTexture.EncodeToPNG();
            string base64Image = Convert.ToBase64String(imageBytes);
            jsonPayload = $"{{\"model\":\"{modelFileName}\",\"messages\":[{{\"role\":\"system\",\"content\":\"{escapedSystemPrompt}\"}},{{\"role\":\"user\",\"content\":[{{\"type\":\"text\",\"text\":\"{escapedText}\"}},{{\"type\":\"image_url\",\"image_url\":{{\"url\":\"data:image/png;base64,{base64Image}\"}}}}]}}],\"temperature\":{tempStr},\"frequency_penalty\":{freqStr},\"presence_penalty\":{presStr},\"stream\":false{grammarJsonField}{maxTokensJsonField}}}";
            statusText = "이미지 분석 및 답변 생성 중...";
        }
        else
        {
            // Pure Text LLM Mode
            jsonPayload = $"{{\"model\":\"{modelFileName}\",\"messages\":[{{\"role\":\"system\",\"content\":\"{escapedSystemPrompt}\"}},{{\"role\":\"user\",\"content\":\"{escapedText}\"}}],\"temperature\":{tempStr},\"frequency_penalty\":{freqStr},\"presence_penalty\":{presStr},\"stream\":false{grammarJsonField}{maxTokensJsonField}}}";
            statusText = "답변 생성 중...";
        }

        Debug.Log($"Sending HTTP request to Llama Server ({serverUrl})...");

        // Start WebRequest Send Coroutine
        StartCoroutine(SendMultimodalRequest(serverUrl, jsonPayload));

        if (responseText != null)
        {
            responseText.text = statusText;
        }

        // Close Confirm Popup
        if (confirmPopup != null)
        {
            confirmPopup.SetActive(false);
        }

        // Clear Chat Input Field
        if (chatInputField != null)
        {
            chatInputField.text = string.Empty;
            chatInputField.ActivateInputField(); // Refocus input field
        }
    }

    private void OnChatInputSubmit(string text)
    {
        // Send on enter key press
        SendImageAndText();
    }

    private IEnumerator SendMultimodalRequest(string url, string jsonPayload)
    {
        using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");

            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Error: {webRequest.error}\nResponse: {webRequest.downloadHandler.text}");
            }
            else
            {
                string jsonResponse = webRequest.downloadHandler.text;
                Debug.Log($"Raw Response: {jsonResponse}");
                try
                {
                    LlamaResponse response = JsonUtility.FromJson<LlamaResponse>(jsonResponse);
                    if (response != null && response.choices != null && response.choices.Length > 0)
                    {
                        string reply = response.choices[0].message.content;
                        OnReplyReceived(reply, response.usage);
                    }
                    else
                    {
                        Debug.LogError("Failed to parse response choices.");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"JSON Parsing Error: {e.Message}");
                }
            }
        }

        OnReplyCompleted();
    }

    private void CancelSend()
    {
        // Close Confirm Popup
        if (confirmPopup != null)
        {
            confirmPopup.SetActive(false);
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        // Restore live camera feed on Android
        if (webcamTexture != null)
        {
            if (photoDisplay != null)
            {
                photoDisplay.texture = webcamTexture;
                photoDisplay.gameObject.SetActive(true);
            }
            webcamTexture.Play();
        }
#else
        // Keep dummy texture visible on Editor
        if (dummyTexture != null && photoDisplay != null)
        {
            photoDisplay.texture = dummyTexture;
            photoDisplay.gameObject.SetActive(false);
        }
#endif
        capturedTexture = null;
        Debug.Log("Send canceled, returned to live camera/dummy view.");
    }

    private void OnReplyReceived(string reply, LlamaResponse.UsageInfo usage)
    {
        string tokenInfo = "";
        if (usage != null)
        {
            tokenInfo = $"\n\n<b>[Token Usage]</b> Prompt: {usage.prompt_tokens} | Completion: {usage.completion_tokens} | Total: {usage.total_tokens}";
        }

        // Combined conversation log for easy copying to AI assistant
        Debug.Log($"<b>[LLM Conversation Log]</b>\n<b>▶ USER:</b> {lastSentPrompt}\n<b>◀ AI:</b> {reply}{tokenInfo}");

        if (responseText != null)
        {
            responseText.text = reply;
        }

        // Parse custom rule patterns from the reply
        ParseRules(reply);
    }

    private void ParseRules(string reply)
    {
        // Find patterns like [ACTION] (Uppercase words enclosed in square brackets)
        var matches = System.Text.RegularExpressions.Regex.Matches(reply, @"\[([A-Z0-9_]+)\]");
        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            string ruleAction = match.Groups[1].Value;
            OnRuleTriggered(ruleAction);
        }
    }

    private void OnRuleTriggered(string rule)
    {
        Debug.Log($"<color=cyan>[RuleEngine Triggered]</color> Detected action rule: <b>{rule}</b>");
        // Implement custom game logic trigger here (e.g. invoking events)
    }

    private void OnReplyCompleted()
    {
        Debug.Log("LLM Reply Completed.");

        if (photoDisplay != null)
        {
            photoDisplay.gameObject.SetActive(false);
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        // Restore live camera feed after reply is completed
        if (webcamTexture != null)
        {
            if (photoDisplay != null)
            {
                photoDisplay.texture = webcamTexture;
                photoDisplay.gameObject.SetActive(true);
            }
            webcamTexture.Play();
        }
#endif
        capturedTexture = null;
    }

    private void OnApplicationQuit()
    {
        // Stop Server when application quits
        StopLlamaServer();
    }

    private void OnDestroy()
    {
        // Release WebCam resources
        if (webcamTexture != null)
        {
            webcamTexture.Stop();
            Destroy(webcamTexture);
        }

        // Final safety stop for server
        StopLlamaServer();
    }
}
*/
