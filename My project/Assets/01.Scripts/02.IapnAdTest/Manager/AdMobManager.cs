using System;
using GoogleMobileAds.Api;
using UnityEngine;

namespace IapnAdTest
{
    public class AdMobManager : MonoBehaviour
    {
        private static AdMobManager _instance;
        private static bool _isShuttingDown = false;

        public static AdMobManager Instance
        {
            get
            {
                if (_isShuttingDown)
                {
                    return null;
                }

                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<AdMobManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("AdMobManager");
                        _instance = go.AddComponent<AdMobManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        // 테스트용 광고 단위 ID (구글 공식 테스트 ID)
#if UNITY_ANDROID
        private const string BannerAdUnitId = "ca-app-pub-3940256099942544/6300978111";
        private const string InterstitialAdUnitId = "ca-app-pub-3940256099942544/1033173712";
#elif UNITY_IOS
        private const string BannerAdUnitId = "ca-app-pub-3940256099942544/2934735716";
        private const string InterstitialAdUnitId = "ca-app-pub-3940256099942544/4411468910";
#else
        private const string BannerAdUnitId = "unused";
        private const string InterstitialAdUnitId = "unused";
#endif

        private BannerView _bannerView;
        private InterstitialAd _interstitialAd;

        public event Action<string> OnAdStatusChanged;

        // 광고 상태 관리 필드
        private bool _isLoadingInterstitial = false;
        private bool _showWhenLoaded = false;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnApplicationQuit()
        {
            _isShuttingDown = true;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _isShuttingDown = true;
                _instance = null;
            }
        }

        private void Start()
        {
            OnAdStatusChanged?.Invoke("SDK 초기화 중...");
            // AdMob SDK 초기화
            MobileAds.Initialize(initStatus =>
            {
                Debug.Log("[AdMob] SDK 초기화 완료");
                OnAdStatusChanged?.Invoke("SDK 초기화 완료");
                // 초기화 완료 후 배너 및 전면 광고 로드
                LoadBannerAd();
                LoadInterstitialAd();
            });
        }

        #region 배너 광고
        public void LoadBannerAd()
        {
            // 기존 배너가 있으면 파괴
            DestroyBannerAd();

            OnAdStatusChanged?.Invoke("배너 광고 로드 중...");

            // 배너 뷰 생성
            _bannerView = new BannerView(BannerAdUnitId, AdSize.Banner, AdPosition.Bottom);

            // 이벤트 등록
            _bannerView.OnBannerAdLoaded += () =>
            {
                Debug.Log("[AdMob] 배너 광고 로드 완료");
                OnAdStatusChanged?.Invoke("배너 광고 로드 완료");
            };
            _bannerView.OnBannerAdLoadFailed += (LoadAdError error) =>
            {
                Debug.LogError($"[AdMob] 배너 광고 로드 실패: {error}");
                OnAdStatusChanged?.Invoke($"배너 광고 로드 실패: {error.GetMessage()}");
            };

            // 광고 요청 생성
            AdRequest adRequest = new AdRequest();

            // 광고 로드
            _bannerView.LoadAd(adRequest);
            Debug.Log("[AdMob] 배너 광고 로드 요청");
        }

        public void DestroyBannerAd()
        {
            if (_bannerView != null)
            {
                _bannerView.Destroy();
                _bannerView = null;
                Debug.Log("[AdMob] 배너 광고 제거");
                OnAdStatusChanged?.Invoke("배너 광고 제거됨");
            }
        }
        #endregion

        #region 전면 광고
        public void LoadInterstitialAd()
        {
            if (_isLoadingInterstitial) return; // 이미 로드 진행 중이면 중복 방지

            // 기존 전면 광고가 있으면 파괴
            if (_interstitialAd != null)
            {
                _interstitialAd.Destroy();
                _interstitialAd = null;
            }

            _isLoadingInterstitial = true;
            OnAdStatusChanged?.Invoke("전면 광고 로드 중...");

            // 광고 요청 생성
            AdRequest adRequest = new AdRequest();

            // 전면 광고 로드
            InterstitialAd.Load(InterstitialAdUnitId, adRequest, (InterstitialAd ad, LoadAdError error) =>
            {
                _isLoadingInterstitial = false;

                if (error != null || ad == null)
                {
                    Debug.LogError($"[AdMob] 전면 광고 로드 실패: {error}");
                    OnAdStatusChanged?.Invoke($"전면 광고 로드 실패: {error.GetMessage()}");

                    // 자동 노출 대기 중이었다면 취소 처리
                    if (_showWhenLoaded)
                    {
                        CancelShowWhenLoaded();
                    }

                    // 5초 후 재시도
                    CancelInvoke(nameof(LoadInterstitialAdInternal));
                    Invoke(nameof(LoadInterstitialAdInternal), 5.0f);
                    return;
                }

                Debug.Log("[AdMob] 전면 광고 로드 완료");
                _interstitialAd = ad;
                OnAdStatusChanged?.Invoke("전면 광고 로드 완료");

                // 광고 생명주기 이벤트 등록
                RegisterInterstitialEvents(_interstitialAd);

                // 사용자 광고 노출 대기 중이었다면 즉시 노출
                if (_showWhenLoaded)
                {
                    _showWhenLoaded = false;
                    CancelInvoke(nameof(CancelShowWhenLoaded));
                    ShowInterstitialAd();
                }
            });
        }

        // Invoke 호출을 위한 래퍼 메서드
        private void LoadInterstitialAdInternal()
        {
            LoadInterstitialAd();
        }

        public void ShowInterstitialAd()
        {
            if (_interstitialAd != null && _interstitialAd.CanShowAd())
            {
                Debug.Log("[AdMob] 전면 광고 표시");
                OnAdStatusChanged?.Invoke("전면 광고 표시");
                _interstitialAd.Show();
            }
            else
            {
                Debug.LogWarning("[AdMob] 전면 광고가 아직 로드되지 않았음. 로딩 후 노출 대기 시작.");
                _showWhenLoaded = true;

                // 5초 후에도 로드가 완료되지 않으면 대기 취소
                CancelInvoke(nameof(CancelShowWhenLoaded));
                Invoke(nameof(CancelShowWhenLoaded), 5.0f);

                OnAdStatusChanged?.Invoke("광고 준비 중... 잠시만 기다려주세요.");

                if (!_isLoadingInterstitial)
                {
                    LoadInterstitialAd();
                }
            }
        }

        private void CancelShowWhenLoaded()
        {
            if (_showWhenLoaded)
            {
                _showWhenLoaded = false;
                Debug.LogWarning("[AdMob] 전면 광고 로드 대기 타임아웃.");
                OnAdStatusChanged?.Invoke("광고 로드 실패 (시간 초과)");
            }
        }

        private void RegisterInterstitialEvents(InterstitialAd ad)
        {
            // 광고 닫혔을 때 다시 로드
            ad.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log("[AdMob] 전면 광고 닫힘 -> 새 전면 광고 로드");
                OnAdStatusChanged?.Invoke("전면 광고 닫힘");
                LoadInterstitialAd();
            };

            ad.OnAdFullScreenContentFailed += (AdError error) =>
            {
                Debug.LogError($"[AdMob] 전면 광고 표시 실패: {error}");
                OnAdStatusChanged?.Invoke($"전면 광고 표시 실패: {error.GetMessage()}");
                LoadInterstitialAd();
            };
        }
        #endregion
    }
}
