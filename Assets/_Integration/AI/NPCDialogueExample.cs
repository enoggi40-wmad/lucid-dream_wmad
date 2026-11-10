using UnityEngine;

namespace LucidDream.AI
{
    /// <summary>
    /// LMStudioClient 사용 예제: NPC가 상황에 맞는 대사를 로컬 LLM으로 즉석 생성한다.
    ///
    /// 사용법:
    ///   1) 씬에 빈 GameObject "LMStudio"를 만들고 LMStudioClient 컴포넌트를 붙인다.
    ///   2) NPC GameObject에 이 컴포넌트를 붙이고 캐릭터 설정을 채운다.
    ///   3) 플레이 모드에서 G 키를 누르거나 RequestLine()을 호출하면 대사가 생성된다.
    ///
    /// 호러 게임 LUCID DREAM 톤에 맞춰, 정신력 수치 등 게임 상태를 프롬프트에 넣어
    /// 상황 반응형 대사를 만들 수 있다.
    /// </summary>
    public class NPCDialogueExample : MonoBehaviour
    {
        [Header("캐릭터 설정 (system 프롬프트로 들어감)")]
        [TextArea(2, 5)]
        public string persona =
            "너는 심리 호러 게임 'LUCID DREAM' 속 폐병원을 떠도는 늙은 경비원 영혼이다. " +
            "말투는 음산하고 짧으며, 플레이어를 경계한다. 1~2문장의 한국어 대사만 말하고 따옴표/설명은 붙이지 마라.";

        [Header("상황 컨텍스트 (user 프롬프트)")]
        [TextArea(2, 5)]
        public string situation =
            "플레이어가 처음으로 경비원과 마주쳤다. 플레이어의 정신력은 낮은 편이다.";

        [Header("입력")]
        public KeyCode triggerKey = KeyCode.G;

        [Header("최근 생성된 대사 (런타임 확인용)")]
        [TextArea(1, 4)]
        public string lastLine;

        private bool _busy;

        private void Update()
        {
            if (Input.GetKeyDown(triggerKey))
                RequestLine();
        }

        /// <summary>현재 persona/situation으로 대사 한 줄을 생성 요청한다.</summary>
        public void RequestLine()
        {
            if (_busy)
                return;

            if (LMStudioClient.Instance == null)
            {
                Debug.LogWarning("[NPCDialogue] 씬에 LMStudioClient가 없습니다. 빈 GameObject에 붙여주세요.");
                return;
            }

            _busy = true;
            LMStudioClient.Instance.Chat(
                user: situation,
                system: persona,
                onSuccess: line =>
                {
                    _busy = false;
                    lastLine = line;
                    Debug.Log($"[NPC 대사] {line}");
                    OnLineGenerated(line);
                },
                onError: err =>
                {
                    _busy = false;
                    Debug.LogWarning("[NPCDialogue] 생성 실패: " + err);
                },
                temp: 0.9f,     // 대사는 다양성 있게
                maxTok: 120
            );
        }

        /// <summary>
        /// 생성된 대사를 받아 실제 게임에 반영하는 지점.
        /// 여기서 말풍선 UI, 자막, TTS 등에 연결하면 된다.
        /// </summary>
        protected virtual void OnLineGenerated(string line)
        {
            // TODO: 말풍선/자막 UI에 line 표시
        }
    }
}
