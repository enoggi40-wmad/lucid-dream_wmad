using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace LucidDream.AI
{
    /// <summary>
    /// LM Studio(OpenAI 호환 로컬 서버, 기본 http://127.0.0.1:1234)에 채팅 요청을 보내는 클라이언트.
    /// 씬에 빈 GameObject를 만들고 이 컴포넌트를 붙인 뒤, 다른 스크립트에서
    /// LMStudioClient.Instance.Chat(...) 으로 호출한다.
    ///
    /// 외부 패키지(Newtonsoft 등) 없이 UnityEngine.JsonUtility만 사용한다.
    /// </summary>
    public class LMStudioClient : MonoBehaviour
    {
        [Header("서버 설정")]
        [Tooltip("LM Studio Local Server 주소 (끝에 /v1 포함)")]
        public string baseUrl = "http://127.0.0.1:1234/v1";

        [Tooltip("LM Studio에 로드된 모델의 API identifier")]
        public string model = "qwen/qwen3.5-9b";

        [Header("생성 파라미터 기본값")]
        [Range(0f, 2f)] public float temperature = 0.7f;
        public int maxTokens = 512;

        [Tooltip("요청 타임아웃(초)")]
        public int timeoutSeconds = 120;

        public static LMStudioClient Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // ───────────────────────────── 공개 API ─────────────────────────────

        /// <summary>
        /// 단발성 프롬프트. system은 선택(역할/세계관 지시).
        /// onSuccess(응답문자열) 또는 onError(에러메시지)가 호출된다.
        /// </summary>
        public void Chat(string user, string system,
                         Action<string> onSuccess, Action<string> onError = null,
                         float? temp = null, int? maxTok = null)
        {
            var messages = new List<ChatMessage>();
            if (!string.IsNullOrEmpty(system))
                messages.Add(new ChatMessage { role = "system", content = system });
            messages.Add(new ChatMessage { role = "user", content = user });
            ChatMessages(messages, onSuccess, onError, temp, maxTok);
        }

        /// <summary>
        /// 멀티턴 대화. messages에 직접 role/content 목록을 넘긴다(대화 맥락 유지용).
        /// </summary>
        public void ChatMessages(List<ChatMessage> messages,
                                 Action<string> onSuccess, Action<string> onError = null,
                                 float? temp = null, int? maxTok = null)
        {
            var req = new ChatRequest
            {
                model = model,
                messages = messages.ToArray(),
                temperature = temp ?? temperature,
                max_tokens = maxTok ?? maxTokens,
                stream = false,
            };
            StartCoroutine(SendRequest(req, onSuccess, onError));
        }

        // ──────────────────────────── 내부 구현 ────────────────────────────

        private IEnumerator SendRequest(ChatRequest req,
                                        Action<string> onSuccess, Action<string> onError)
        {
            string url = baseUrl.TrimEnd('/') + "/chat/completions";
            string json = JsonUtility.ToJson(req);
            byte[] body = Encoding.UTF8.GetBytes(json);

            using (var www = new UnityWebRequest(url, "POST"))
            {
                www.uploadHandler = new UploadHandlerRaw(body);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");
                www.timeout = timeoutSeconds;

                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    string err = $"LM Studio 요청 실패: {www.error} (code {www.responseCode})";
                    Debug.LogWarning("[LMStudioClient] " + err);
                    onError?.Invoke(err);
                    yield break;
                }

                string text = www.downloadHandler.text;
                string content = ParseContent(text);
                if (content == null)
                {
                    string err = "응답 파싱 실패: " + Truncate(text, 300);
                    Debug.LogWarning("[LMStudioClient] " + err);
                    onError?.Invoke(err);
                    yield break;
                }
                onSuccess?.Invoke(content.Trim());
            }
        }

        private static string ParseContent(string responseJson)
        {
            try
            {
                var parsed = JsonUtility.FromJson<ChatResponse>(responseJson);
                if (parsed?.choices != null && parsed.choices.Length > 0)
                    return parsed.choices[0].message.content;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[LMStudioClient] JSON 예외: " + e.Message);
            }
            return null;
        }

        private static string Truncate(string s, int n) =>
            string.IsNullOrEmpty(s) || s.Length <= n ? s : s.Substring(0, n) + "...";

        // ──────────────────── 직렬화용 DTO (JsonUtility) ────────────────────

        [Serializable]
        public class ChatMessage
        {
            public string role;
            public string content;
        }

        [Serializable]
        private class ChatRequest
        {
            public string model;
            public ChatMessage[] messages;
            public float temperature;
            public int max_tokens;
            public bool stream;
        }

        [Serializable]
        private class ChatResponse
        {
            public ChatChoice[] choices;
        }

        [Serializable]
        private class ChatChoice
        {
            public ChatMessage message;
        }
    }
}
