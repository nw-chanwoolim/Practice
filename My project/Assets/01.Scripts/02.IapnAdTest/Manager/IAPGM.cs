using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Networking; // 서버 통신용 임포트 추가
#if UNITY_ANDROID || UNITY_IOS
using UnityEngine.Purchasing.Security;
#endif

namespace Iap.Test
{
    public class IAPGM : MonoBehaviour
    {
        public static IAPGM Instance { get; private set; }

        [Header("IAP Event Channel")]
        [SerializeField] private IAPEventChannelSO m_IapEventChannel; // 외부 전파용 이벤트 채널 에셋

        private StoreController m_StoreController; // 구매 과정을 제어하는 스토어 컨트롤러
        private bool m_IsConnected = false;        // 스토어 연결 성공 여부 플래그
        private bool m_IsPurchasing = false;       // 현재 구매 요청이 진행 중인지 여부 (중복 구매 방지용)

        // 스토어 상품 ID 정의 (Google Play Console 및 App Store Connect에 등록할 ID와 동일해야 합니다.)
        public const string ProductGold100 = "com.iaptest.testing.gold100"; // 소모성 상품 예시
        public const string ProductNoAds = "com.iaptest.testing.noads";     // 비소모성 상품 예시

        // ==========================================
        // [서버 검증용 DTO 및 내부 영수증 구조 정의]
        // ==========================================
#pragma warning disable CS0649 // JsonUtility 역직렬화 필드 미할당 경고 억제
        [Serializable]
        public class VerifyRequestDTO
        {
            public string product_id;
            public string player_id;
            // iOS 전용
            public string receipt_data;
            public string transaction_id;
            // Android 전용
            public string type; // "product" or "subscription"
            public string package_name;
            public string purchase_token;
        }

        [Serializable]
        public class VerifyResponseDTO
        {
            public bool success;
            public string message;
            public int status;
        }

        [Serializable]
        private class UnityReceipt { public string Store; public string Payload; }
        [Serializable]
        private class GooglePlayPayload { public string json; public string signature; }
        [Serializable]
        private class GooglePlayJson { public string purchaseToken; public string productId; public string packageName; }
#pragma warning restore CS0649 // 경고 억제 복원

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializePurchasing();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        // 결제 시스템 초기화
        public async void InitializePurchasing()
        {
            if (m_IsConnected) return;

            try
            {
                m_StoreController = UnityIAPServices.StoreController();

                // 저수준 이벤트 핸들러 구독
                m_StoreController.OnProductsFetched += OnProductsFetched;
                m_StoreController.OnProductsFetchFailed += OnProductsFetchFailed;
                m_StoreController.OnPurchasePending += OnPurchasePending;
                m_StoreController.OnPurchaseFailed += OnPurchaseFailed;
                m_StoreController.OnStoreDisconnected += OnStoreDisconnected;

                // 스토어 연결 비동기 처리
                await m_StoreController.Connect();
                m_IsConnected = true;
                Debug.Log("IAPGM: Unity IAP 스토어 연결 성공.");

                // 이벤트 채널을 통해 성공 알림 전파
                if (m_IapEventChannel != null)
                {
                    m_IapEventChannel.RaiseConnectionSuccess();
                }

                // 상품 목록 조회
                FetchProducts();
            }
            catch (Exception ex)
            {
                Debug.LogError($"IAPGM: Unity IAP 초기화 중 오류 발생: {ex.Message}");
                m_IsConnected = false;
            }
        }

        // 상품 목록 조회 처리
        private void FetchProducts()
        {
            if (!m_IsConnected) return;

            List<ProductDefinition> productsToFetch = new()
            {
                new(ProductGold100, ProductType.Consumable),
                new(ProductNoAds, ProductType.NonConsumable)
            };

            m_StoreController.FetchProducts(productsToFetch);
        }

        private bool IsInitialized()
        {
            return m_IsConnected && m_StoreController != null;
        }

        // 특정 상품 구매 요청 호출 함수
        public void BuyProductID(string productId)
        {
            if (!IsInitialized())
            {
                Debug.LogError("IAPGM: 구매 실패 - IAP 시스템이 초기화되지 않았습니다.");
                return;
            }

            if (m_IsPurchasing)
            {
                Debug.LogWarning("IAPGM: 이미 구매 요청이 진행 중입니다. 잠시만 기다려 주세요.");
                return;
            }

            Debug.Log($"IAPGM: 상품 구매 요청 시작: '{productId}'");
            m_IsPurchasing = true; // 결제 진행 중 플래그 활성화 (중복 결제 방지)
            m_StoreController.PurchaseProduct(productId);
        }

        // 비소모성 상품 또는 구독 복구 (iOS 필수)
        public void RestorePurchases()
        {
            if (!IsInitialized())
            {
                Debug.LogError("IAPGM: 복구 실패 - IAP 시스템이 초기화되지 않았습니다.");
                return;
            }

            if (Application.platform == RuntimePlatform.IPhonePlayer ||
                Application.platform == RuntimePlatform.OSXPlayer)
            {
                Debug.Log("IAPGM: iOS 구매 복구 시작...");
                m_StoreController.RestoreTransactions((success, error) =>
                {
                    if (success)
                    {
                        Debug.Log("IAPGM: 구매 복구 요청 성공!");
                    }
                    else
                    {
                        Debug.LogError($"IAPGM: 구매 복구 실패: {error}");
                    }
                });
            }
        }

        private void OnDestroy()
        {
            // 메모리 누수 방지를 위한 이벤트 구독 해제
            if (m_StoreController != null)
            {
                m_StoreController.OnProductsFetched -= OnProductsFetched;
                m_StoreController.OnProductsFetchFailed -= OnProductsFetchFailed;
                m_StoreController.OnPurchasePending -= OnPurchasePending;
                m_StoreController.OnPurchaseFailed -= OnPurchaseFailed;
                m_StoreController.OnStoreDisconnected -= OnStoreDisconnected;
            }
        }

        #region IAP 저수준 이벤트 핸들러 및 채널 전파

        // 상품 목록 조회 완료 시 호출
        private void OnProductsFetched(List<Product> products)
        {
            Debug.Log($"IAPGM: 상품 목록 조회 완료. 조회된 상품 수: {products.Count}");
            if (m_IapEventChannel != null)
            {
                m_IapEventChannel.RaiseProductsLoaded(products);
            }
        }

        // 상품 목록 조회 실패 시 호출
        private void OnProductsFetchFailed(ProductFetchFailed error)
        {
            Debug.LogError($"IAPGM: 상품 목록 조회 실패. 이유: {error.FailureReason}");
            if (m_IapEventChannel != null)
            {
                m_IapEventChannel.RaiseProductsLoadedFailed(error);
            }
        }

        // 스토어와의 연결이 끊겼을 때 호출
        private void OnStoreDisconnected(StoreConnectionFailureDescription error)
        {
            Debug.LogError($"IAPGM: Unity IAP 스토어 연결 끊김: {error.Message} (재시도 가능 여부: {error.IsRetryable})");
            m_IsConnected = false;
            m_IsPurchasing = false; // 스토어 단절 시 구매 잠금 해제
            if (m_IapEventChannel != null)
            {
                m_IapEventChannel.RaiseConnectionFailed(error);
            }
        }

        // 구매 진행 중 시 호출
        private void OnPurchasePending(PendingOrder pendingOrder)
        {
            string transactionId = pendingOrder.Info.TransactionID;
            Debug.Log($"IAPGM: 결제 완료 신호 수신 (Pending) - TxID: {transactionId}");

            // =========================================================================================
            // [서버 검증으로의 교체 방법]
            // 1. 아래 로컬 검증 영역(1번 중복 방지, 2번 로컬 영수증 검증, 3번 GrantReward) 전체를 주도적으로 주석처리합니다.
            // 2. 대신 하단에 주석처리되어 있는 'VerifyReceiptOnServerAsync(pendingOrder).Forget();'의 호출 주석을 푸세요.
            // =========================================================================================

            // --- 로컬 검증 영역 (시작) ---

            // 1. 이미 처리 완료된 중복 결제(영수증 재사용 공격) 방지
            if (IsTransactionAlreadyProcessed(transactionId))
            {
                m_IsPurchasing = false;
                Debug.LogWarning($"IAPGM: 이미 처리 완료된 거래(TxID: {transactionId})입니다. 보상 지급이 취소됩니다.");
                m_StoreController.ConfirmPurchase(pendingOrder);
                return;
            }

#if UNITY_ANDROID
            // 2. 안드로이드인 경우 로컬 영수증 교차 서명 검증 수행
            if (!ValidateReceipt(pendingOrder.Info.Receipt))
            {
                m_IsPurchasing = false; // 구매 잠금 해제
                Debug.LogError("IAPGM: 영수증 보안 검증 실패로 결제 처리가 중단됩니다.");
                return;
            }
#endif

            // 3. 비즈니스 로직(실제 재화 지급 및 완료 통보) 실행
            GrantReward(pendingOrder);

            // --- 로컬 검증 영역 (끝) ---


            /*
            // --- 서버 검증 교체 시 사용예시 ---
            // 로컬 검증 코드를 모두 지우고(혹은 주석처리) 아래 비동기 메소드 호출로 대체합니다.
            _ = VerifyReceiptOnServerAsync(pendingOrder);
            */
        }

        // 구매 실패 시 호출
        private void OnPurchaseFailed(FailedOrder failedOrder)
        {
            string productId = "Unknown";
            foreach (var item in failedOrder.CartOrdered.Items())
            {
                productId = item.Product.definition.id;
                break;
            }

            Debug.LogError($"IAPGM: 구매 실패 - 상품: '{productId}', 이유: {failedOrder.FailureReason}, 설명: {failedOrder.Details}");
            m_IsPurchasing = false; // 구매 실패 시 구매 잠금 해제

            if (m_IapEventChannel != null)
            {
                m_IapEventChannel.RaisePurchaseFailed(failedOrder);
            }
        }

        #endregion

        #region 비즈니스 로직 (실제 재화 지급 및 확정 처리)

        // 비즈니스 로직: 영수증 검증 성공 후 실제 아이템 지급 및 최종 승인
        private void GrantReward(PendingOrder pendingOrder)
        {
            bool isGrantSuccess = false;
            try
            {
                // 장바구니에 담긴 모든 결제 완료 상품 지급
                foreach (var item in pendingOrder.CartOrdered.Items())
                {
                    string purchasedProductId = item.Product.definition.id;
                    Debug.Log($"IAPGM: 보상 아이템 지급 처리 중 - {purchasedProductId}");

                    if (string.Equals(purchasedProductId, ProductGold100, StringComparison.Ordinal))
                    {
                        Debug.Log("IAPGM: 골드 100개 지급 완료!");
                        // TODO: 인게임 인벤토리/재화 매니저 연동
                    }
                    else if (string.Equals(purchasedProductId, ProductNoAds, StringComparison.Ordinal))
                    {
                        Debug.Log("IAPGM: 광고 제거 활성화!");
                        // TODO: 광고 제거 활성화 상태 저장
                    }
                }

                // 모든 상품 보상이 에러 없이 안정적으로 지급된 경우 성공 처리
                isGrantSuccess = true;

                // 외부 컴포넌트(UI 팝업 등)에 결제 성공 이벤트 최종 전파
                if (m_IapEventChannel != null)
                {
                    m_IapEventChannel.RaisePurchaseSuccess(pendingOrder);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"IAPGM: 보상 지급 처리 중 예외 발생. 결제 완료를 연기합니다. 에러: {ex.Message}");
            }
            finally
            {
                if (isGrantSuccess)
                {
                    // 1. 거래 번호 완료 내역 저장 (영수증 재사용 차단)
                    SaveProcessedTransaction(pendingOrder.Info.TransactionID);

                    // 2. 거래 완료를 스토어에 통보 (반드시 보상 지급 완료 후 호출)
                    m_StoreController.ConfirmPurchase(pendingOrder);
                    Debug.Log("IAPGM: 영수증 최종 확정 완료 (ConfirmPurchase)");
                }
                else
                {
                    // 보상 지급 실패 시에는 확정을 유보하여, 앱 재실행 시 스토어 결제 콜백이 다시 불리도록 유도
                    Debug.LogWarning("IAPGM: 보상 지급 실패로 인해 영수증 확정을 유보합니다. (미확정 주문 상태 유지)");
                }

                // 3. 구매 프로세스 상태 잠금 해제
                m_IsPurchasing = false;
            }
        }

        // 이미 처리된 결제 영수증인지 고유 ID 대조 확인 (Replay Attack 차단)
        private bool IsTransactionAlreadyProcessed(string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId)) return false;

            // 기기 고유 솔트값을 더해 해시화하여 PlayerPrefs 변조 난이도 향상
            string salt = SystemInfo.deviceUniqueIdentifier ?? "DefaultIAPSalt";
            string rawKey = transactionId + salt;

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawKey));
                string hexHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

                return PlayerPrefs.HasKey("IAP_TX_" + hexHash);
            }
        }

        // 보상 지급 완료 후 해당 고유 거래 번호 저장
        private void SaveProcessedTransaction(string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId)) return;

            string salt = SystemInfo.deviceUniqueIdentifier ?? "DefaultIAPSalt";
            string rawKey = transactionId + salt;

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawKey));
                string hexHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

                PlayerPrefs.SetInt("IAP_TX_" + hexHash, 1);
                PlayerPrefs.Save();
            }
        }

#if UNITY_ANDROID
        // 영수증 서명 유효성 교차 검증 함수
        private bool ValidateReceipt(string receiptJson)
        {
            // 유니티 에디터(PC 테스트) 환경에서는 모조 결제창이 뜨므로 영수증 검증을 항상 패스합니다.
            if (Application.isEditor)
            {
                Debug.Log("IAPGM: PC 에디터 환경 테스트이므로 영수증 검증을 생략합니다.");
                return true;
            }

            try
            {
                // GooglePlayTangle 클래스가 없는 상태에서 빌드 락(컴파일 에러)이 걸리지 않도록 리플렉션을 사용합니다.
                Type googlePlayTangleType = Type.GetType("UnityEngine.Purchasing.GooglePlayTangle")
                                            ?? Type.GetType("GooglePlayTangle");

                if (googlePlayTangleType == null)
                {
                    Debug.LogWarning("IAPGM: GooglePlayTangle 클래스를 찾을 수 없습니다. Window > Unity IAP > Receipt Validation Obfuscator 메뉴를 통해 키를 먼저 생성해 주세요. 현재는 임시로 검증을 통과시킵니다.");
                    return true;
                }

                var dataMethod = googlePlayTangleType.GetMethod("Data");
                if (dataMethod == null)
                {
                    Debug.LogError("IAPGM: GooglePlayTangle.Data() 메소드를 찾을 수 없습니다.");
                    return false;
                }

                byte[] googlePlayTangleData = (byte[])dataMethod.Invoke(null, null);

                var validator = new CrossPlatformValidator(
                    googlePlayTangleData,
                    null, // Apple 로컬 검증은 v5부터 미지원하므로 null 전달
                    Application.identifier
                );

                // 검증 수행 (예외 발생 시 위조 또는 잘못된 서명)
                validator.Validate(receiptJson);
                Debug.Log("IAPGM: 로컬 영수증 서명 검증 완료 (신뢰할 수 있는 결제)");
                return true;
            }
            catch (IAPSecurityException ex)
            {
                Debug.LogError($"IAPGM: 로컬 영수증 검증 실패 (보안 경고: 위조 영수증 의심) - {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"IAPGM: 영수증 검증 과정 중 오류 발생 - {ex.Message}");
                return false;
            }
        }
#endif
        #endregion

        #region 서버 검증 스켈레톤 코드 (추후 전환용)
        // =========================================================================
        // [서버 사이드 검증 스켈레톤 코드 (추후 전환용)]
        // =========================================================================
        /*
        private async Task VerifyReceiptOnServerAsync(PendingOrder pendingOrder)
        {
            string url = Application.platform == RuntimePlatform.IPhonePlayer 
                ? "https://copyit.nwaple.com/ios_purchase_check.php" // 예시 iOS 주소
                : "https://copyit.nwaple.com/android_purchase_check.php"; // 예시 안드로이드 주소

            VerifyRequestDTO requestBody = new VerifyRequestDTO
            {
                product_id = pendingOrder.CartOrdered.Items()[0].Product.definition.id, // 첫 상품 기준 예시
                player_id = "USER_PLAYER_ID" // TODO: 실제 인증 서비스의 PlayerID로 연동
            };

            try
            {
                // 플랫폼별 데이터 파싱 및 채우기
                if (Application.platform == RuntimePlatform.Android)
                {
                    var unityReceipt = JsonUtility.FromJson<UnityReceipt>(pendingOrder.Info.Receipt);
                    var googlePayload = JsonUtility.FromJson<GooglePlayPayload>(unityReceipt.Payload);
                    var googleJson = JsonUtility.FromJson<GooglePlayJson>(googlePayload.json);

                    requestBody.type = "product";
                    requestBody.package_name = googleJson.packageName;
                    requestBody.purchase_token = googleJson.purchaseToken;
                }
                else if (Application.platform == RuntimePlatform.IPhonePlayer)
                {
                    var unityReceipt = JsonUtility.FromJson<UnityReceipt>(pendingOrder.Info.Receipt);
                    string rawReceipt = unityReceipt != null ? unityReceipt.Payload : pendingOrder.Info.Receipt;

                    requestBody.receipt_data = rawReceipt;
                    requestBody.transaction_id = pendingOrder.Info.TransactionID;
                }
                else
                {
                    Debug.LogWarning("IAPGM: 서버 검증 - 지원하지 않는 플랫폼입니다. 로컬 결제를 통과시킵니다.");
                    GrantReward(pendingOrder);
                    return;
                }

                // POST 요청 바디 생성
                string jsonRequestBody = JsonUtility.ToJson(requestBody);
                using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
                {
                    byte[] jsonToSend = new UTF8Encoding().GetBytes(jsonRequestBody);
                    webRequest.uploadHandler = new UploadHandlerRaw(jsonToSend);
                    webRequest.downloadHandler = new DownloadHandlerBuffer();
                    webRequest.SetRequestHeader("Content-Type", "application/json");

                    // 서버 비동기 전송
                    var operation = webRequest.SendWebRequest();
                    while (!operation.isDone)
                    {
                        await Task.Yield();
                    }

                    if (webRequest.result == UnityWebRequest.Result.Success)
                    {
                        var response = JsonUtility.FromJson<VerifyResponseDTO>(webRequest.downloadHandler.text);
                        if (response != null && response.success)
                        {
                            Debug.Log($"IAPGM: [Server Verify] 검증 성공! - {response.message}");
                            
                            // 보상 지급 및 영수증 확정
                            GrantReward(pendingOrder);
                        }
                        else
                        {
                            string msg = response != null ? response.message : "No Response";
                            Debug.LogError($"IAPGM: [Server Verify] 검증 실패: {msg}");
                            
                            // 영수증 확정을 안 하고 보상 락을 해제합니다. (앱 재시작 시 재시도 호출됨)
                            m_IsPurchasing = false; 
                        }
                    }
                    else
                    {
                        Debug.LogError($"IAPGM: [Server Verify] 서버 네트워크 오류: {webRequest.error}");
                        m_IsPurchasing = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"IAPGM: [Server Verify] 검증 비동기 파싱 예외: {ex.Message}");
                m_IsPurchasing = false;
            }
        }
        */
        #endregion
    }
}