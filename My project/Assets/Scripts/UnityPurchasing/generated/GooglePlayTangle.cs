// WARNING: Do not modify! Generated file.

namespace UnityEngine.Purchasing.Security {
    public class GooglePlayTangle
    {
        private static byte[] data = System.Convert.FromBase64String("AIONgrIAg4iAAIODghn70uXgr1wFXx5KCPx2YEA32pBq40Vbqs0JNSJorLPltsBa4xg82s+oiBMgnpAdLzwQIZCmXYtNLK57UxexYKa0KixcIQ+KZptP8f6eBYGtKEJ5wJ3q46jUYSRLujtuiVD4KhQsSWCfAZq8sgCDoLKPhIuoBMoEdY+Dg4OHgoHOQDVOikkjbBcmKRtALU1WT7uaxjlez0uLASdtTQ7wFie/E6Ethyj+xxpQRdExAECR2zTWXUk8/LA47EqVKdriZUKBAd0rLCXGwif9aYvsmIbunaCrJrGGvU/j+E5KHATJ7VtRC6gXIpjj+J8GigfLdwKCP7cctaU5hMpidIQeuS0RZ66qBGX3umTTzZmILc3D+I40N4CBg4KD");
        private static int[] order = new int[] { 1,8,4,13,5,10,8,8,11,9,11,11,12,13,14 };
        private static int key = 130;

        public static readonly bool IsPopulated = true;

        public static byte[] Data() {
        	if (IsPopulated == false)
        		return null;
            return Obfuscator.DeObfuscate(data, order, key);
        }
    }
}
