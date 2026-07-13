using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Purchasing;

namespace Iap.Test
{
    public class IAPTestUI : MonoBehaviour
    {
        [Header("IAP 이벤트 채널")]
        [SerializeField] private IAPEventChannelSO m_IapEventChannel;

        [Header("구매 버튼")]
        [SerializeField] private Button m_BtnGold100;
        [SerializeField] private Button m_BtnNoAds;

        [Header("상태 표시 UI")]
        [SerializeField] private TextMeshProUGUI m_TxtStatus;

        private void OnEnable()
        {
            // 이벤트 구독 및 버튼 리스너 바인딩
            if (m_IapEventChannel != null)
            {
                m_IapEventChannel.OnConnectionSuccess += OnIapConnected;
                m_IapEventChannel.OnConnectionFailed += OnIapConnectFailed;
                m_IapEventChannel.OnPurchaseSuccess += OnPurchaseSuccess;
                m_IapEventChannel.OnPurchaseFailed += OnPurchaseFailed;
            }

            if (m_BtnGold100 != null)
                m_BtnGold100.onClick.AddListener(OnClickedGold100);

            if (m_BtnNoAds != null)
                m_BtnNoAds.onClick.AddListener(OnClickedNoAds);
        }

        private void OnDisable()
        {
            // 이벤트 구독 해제 및 리스너 해제
            if (m_IapEventChannel != null)
            {
                m_IapEventChannel.OnConnectionSuccess -= OnIapConnected;
                m_IapEventChannel.OnConnectionFailed -= OnIapConnectFailed;
                m_IapEventChannel.OnPurchaseSuccess -= OnPurchaseSuccess;
                m_IapEventChannel.OnPurchaseFailed -= OnPurchaseFailed;
            }

            if (m_BtnGold100 != null)
                m_BtnGold100.onClick.RemoveListener(OnClickedGold100);

            if (m_BtnNoAds != null)
                m_BtnNoAds.onClick.RemoveListener(OnClickedNoAds);
        }

        private void Start()
        {
            UpdateStatus("IAP 시스템 초기화 대기 중...");
        }

        // 골드 100개 구매 클릭 시
        private void OnClickedGold100()
        {
            UpdateStatus("골드 100개 구매 요청 중...");
            IAPGM.Instance.BuyProductID(IAPGM.ProductGold100);
        }

        // 광고 제거 구매 클릭 시
        private void OnClickedNoAds()
        {
            UpdateStatus("광고 제거 구매 요청 중...");
            IAPGM.Instance.BuyProductID(IAPGM.ProductNoAds);
        }

        // 스토어 연결 성공 시
        private void OnIapConnected()
        {
            UpdateStatus("스토어 연결 성공! 결제 준비 완료.");
        }

        // 스토어 연결 실패 시
        private void OnIapConnectFailed(StoreConnectionFailureDescription error)
        {
            UpdateStatus($"스토어 연결 실패: {error.Message}");
        }

        // 구매 성공 및 보상 지급 완료 시
        private void OnPurchaseSuccess(PendingOrder pendingOrder)
        {
            string productIds = "";
            foreach (var item in pendingOrder.CartOrdered.Items())
            {
                productIds += item.Product.definition.id + " ";
            }
            UpdateStatus($"구매 완료! 보상 지급됨: {productIds}");
        }

        // 구매 실패 시
        private void OnPurchaseFailed(FailedOrder failedOrder)
        {
            UpdateStatus($"구매 실패: {failedOrder.FailureReason}");
        }

        // UI 텍스트 및 로그 갱신
        private void UpdateStatus(string message)
        {
            Debug.Log($"[IAPUI] {message}");
            if (m_TxtStatus != null)
            {
                m_TxtStatus.text = message;
            }
        }
    }
}
