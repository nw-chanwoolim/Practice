using UnityEngine;
using Practice.Base;
using Unity.Services.Core;
using Unity.Services.Authentication;
using System;
using System.Threading.Tasks;

namespace Practice.Play
{
    public class UgsManager : ManagerBase<UgsManager>
    {
        public bool IsInitalized { get; private set; }
        public event Action<bool> OnInitialized;

        protected override async void Awake()
        {
            base.Awake();
            await InitializeUgsAsync();
        }

        private async Task InitializeUgsAsync()
        {
            if(IsInitalized)
            {
                Debug.LogWarning("Unity Gaming Services is already initialized.");
                return;
            }
            try
            {
                await UnityServices.InitializeAsync();
                 if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }
                IsInitalized = true;
                OnInitialized?.Invoke(true);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to initialize Unity Gaming Services: {e.Message}");
                IsInitalized = false;
                OnInitialized?.Invoke(false);
            }
            Debug.Log("Initialized Unity Gaming Services.");
        }

        public void RequestUgsInitialization()
        {
            if (!IsInitalized)
            {
               _ = InitializeUgsAsync();
            }
        }
    }
}