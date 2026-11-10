# AI 연동 (LM Studio)

Unity에서 로컬 LLM(LM Studio, OpenAI 호환 `http://127.0.0.1:1234/v1`)을 호출하는 모듈.
외부 패키지 없이 `UnityWebRequest` + `JsonUtility`만 사용한다. 네임스페이스 `LucidDream.AI`.

## 파일
- **LMStudioClient.cs** — 재사용 클라이언트(싱글톤 MonoBehaviour). 씬의 빈 GameObject에 붙이고
  `LMStudioClient.Instance.Chat(user, system, onSuccess, onError, ...)` 로 호출.
- **NPCDialogueExample.cs** — NPC가 상황 맞춤 대사를 즉석 생성하는 예제(플레이 모드에서 G키).

## 빠른 시작
1. 빈 GameObject `LMStudio` 생성 → `LMStudioClient` 추가.
2. NPC에 `NPCDialogueExample` 추가 → persona/situation 작성 → ▶ → G키.

## 호출 예
```csharp
using LucidDream.AI;

LMStudioClient.Instance.Chat(
    user:   $"정신력 {sanity}/100, 배회체 접근 중. 짧은 혼잣말 한 줄.",
    system: "너는 LUCID DREAM 주인공이다. 한국어 1문장, 따옴표 없이.",
    onSuccess: line => subtitle.Show(line),   // 메인 스레드에서 호출됨
    onError:   err  => Debug.LogWarning(err));
```

## 주의
- 비동기(코루틴) — 게임 안 멈춤. 응답은 콜백으로.
- **배포 빌드에는 LM Studio 서버가 없으므로** 개발/프로토타입용. 생성 결과는 미리 뽑아
  ScriptableObject 등으로 굳혀 배포할 것.

전체 가이드: `C:\Users\Administrator\LM_STUDIO_연동_가이드.md`
