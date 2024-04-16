using UnityEngine;

/// <summary>
/// 调用 Android 一些方法的整理
/// </summary>
public static class AndroidToast
{
#if UNITY_ANDROID
    /// <summary>
    /// 第二种方式 Toast 数据
    /// </summary>
    /// <param name="str">数据内容</param>
    public static void ToastStringShow(object str, AndroidJavaObject activity = null)
    {
        if (activity == null)
        {
            AndroidJavaClass UnityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            activity = UnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        }

        AndroidJavaClass Toast = new AndroidJavaClass("android.widget.Toast");
        AndroidJavaObject context = activity.Call<AndroidJavaObject>("getApplicationContext");
        activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
        {
            AndroidJavaObject javaString = new AndroidJavaObject("java.lang.String", str.ToString());
            Toast.CallStatic<AndroidJavaObject>("makeText", context, javaString, Toast.GetStatic<int>("LENGTH_SHORT")).Call("show");
        }
        ));
    }
#endif
}
//相关说明
//a.AndroidJavaClass对应着Android里面的Java类，而AndroidJavaObject对应着Android里面实例化的对象。
//b.一定要切记C#里的String和Java的String不是一码事，所以调用Android方法时如果需要传字符串为参数时，不能直接给个字符串，应该给个Java里的String，例如 new AndroidJavaObject("java.lang.String","你想传的字符串");
//c.由于AndroidJavaClass对应的是类，所以一般用之来调用对应的类的静态变量（GetStatic<Type>）或者静态方法(CallStatic<Type>("functionName", param1, param2,....));其中的Type为返回类型，注意是Java的返回类型不是C#的，一般整型和布尔型是通用的，其他的如果不清除可以统一写返回类型为AndroidJavaObject，当然没有返回类型的不需要写Type。
//d.AndroidJavaObject对应的是实例对象，所以用new方法给其初始化时要注意说明其是哪个类的实例对象。再比如刚才那个例子： AndroidJavaObject javaString = new AndroidJavaObject("java.lang.String", "字符串的值");

