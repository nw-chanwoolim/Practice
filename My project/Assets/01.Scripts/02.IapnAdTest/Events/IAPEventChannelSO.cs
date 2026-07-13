using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

namespace Iap.Test
{
    [CreateAssetMenu(fileName = "IAPEventChannelSO", menuName = "IAP/IAPEventChannelSO")]
    public class IAPEventChannelSO : ScriptableObject
    {
        // 외부(UI, 인벤토리 등)에서 구독할 고수준 결제 이벤트 정의 (순수 브리지)
        public event Action OnConnectionSuccess;                                    // 스토어 연결 성공 시
        public event Action<StoreConnectionFailureDescription> OnConnectionFailed;    // 스토어 연결 실패 시
        public event Action<List<Product>> OnProductsLoaded;                         // 상품 정보 로드 완료 시
        public event Action<ProductFetchFailed> OnProductsLoadedFailed;              // 상품 정보 로드 실패 시
        public event Action<PendingOrder> OnPurchaseSuccess;                        // 결제 성공(보상 지급 단계) 시
        public event Action<FailedOrder> OnPurchaseFailed;                           // 결제 실패 시

        // 매니저(IAPGM) 측에서 이벤트를 발생시키기 위해(Raise) 호출하는 메소드들
        public void RaiseConnectionSuccess() => OnConnectionSuccess?.Invoke();
        public void RaiseConnectionFailed(StoreConnectionFailureDescription error) => OnConnectionFailed?.Invoke(error);
        public void RaiseProductsLoaded(List<Product> products) => OnProductsLoaded?.Invoke(products);
        public void RaiseProductsLoadedFailed(ProductFetchFailed error) => OnProductsLoadedFailed?.Invoke(error);
        public void RaisePurchaseSuccess(PendingOrder pendingOrder) => OnPurchaseSuccess?.Invoke(pendingOrder);
        public void RaisePurchaseFailed(FailedOrder failedOrder) => OnPurchaseFailed?.Invoke(failedOrder);
    }
}
