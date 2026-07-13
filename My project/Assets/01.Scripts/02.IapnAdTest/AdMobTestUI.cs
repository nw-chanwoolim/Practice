using IapnAdTest;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ad.Test
{
    public class AdMobTestUI : MonoBehaviour
    {
        [Header("AdMob 제어 버튼")]
        [SerializeField] private Button m_BtnLoadBanner;
        [SerializeField] private Button m_BtnDestroyBanner;
        [SerializeField] private Button m_BtnShowInterstitial;

        [Header("상태 표시 UI")]
        [SerializeField] private TextMeshProUGUI m_TxtStatus;

        private void OnEnable()
        {
            // AdMob 상태 변경 이벤트 구독
            if (AdMobManager.Instance != null)
                AdMobManager.Instance.OnAdStatusChanged += UpdateStatus;

            // 버튼 클릭 이벤트 리스너 등록
            if (m_BtnLoadBanner != null)
                m_BtnLoadBanner.onClick.AddListener(OnClickedLoadBanner);

            if (m_BtnDestroyBanner != null)
                m_BtnDestroyBanner.onClick.AddListener(OnClickedDestroyBanner);

            if (m_BtnShowInterstitial != null)
                m_BtnShowInterstitial.onClick.AddListener(OnClickedShowInterstitial);
        }

        private void OnDisable()
        {
            // AdMob 상태 변경 이벤트 해제
            if (AdMobManager.Instance != null)
                AdMobManager.Instance.OnAdStatusChanged -= UpdateStatus;

            // 버튼 클릭 이벤트 리스너 해제
            if (m_BtnLoadBanner != null)
                m_BtnLoadBanner.onClick.RemoveListener(OnClickedLoadBanner);

            if (m_BtnDestroyBanner != null)
                m_BtnDestroyBanner.onClick.RemoveListener(OnClickedDestroyBanner);

            if (m_BtnShowInterstitial != null)
                m_BtnShowInterstitial.onClick.RemoveListener(OnClickedShowInterstitial);
        }

        private void Start()
        {
            UpdateStatus("AdMob 테스트 UI 준비 완료.");
        }

        // 배너 광고 로드 버튼 클릭 시
        private void OnClickedLoadBanner()
        {
            UpdateStatus("배너 광고 로드 요청...");
            AdMobManager.Instance.LoadBannerAd();
        }

        // 배너 광고 제거 버튼 클릭 시
        private void OnClickedDestroyBanner()
        {
            UpdateStatus("배너 광고 제거 요청...");
            AdMobManager.Instance.DestroyBannerAd();
        }

        // 전면 광고 표시 버튼 클릭 시
        private void OnClickedShowInterstitial()
        {
            UpdateStatus("전면 광고 표시 요청...");
            AdMobManager.Instance.ShowInterstitialAd();
        }

        // UI 상태 메시지 및 로그 출력
        private void UpdateStatus(string message)
        {
            Debug.Log($"[AdMobUI] {message}");
            if (m_TxtStatus != null)
            {
                m_TxtStatus.text = message;
            }
        }
    }
}
