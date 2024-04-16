using Android.BLE;
using Android.BLE.Commands;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Android;

public class ConfigMain : MonoBehaviour
{
    private string[] blePermissions = {
            "android.permission.BLUETOOTH_SCAN" ,
            "android.permission.ACCESS_FINE_LOCATION",
            "android.permission.ACCESS_COARSE_LOCATION",
            "android.permission.ACCESS_LOCATION_EXTRA_COMMANDS",
            "android.permission.BLUETOOTH_CONNECT"
    };
    private int grantedCount = 0;
    private const string  bleKey = "epy-water";
    private string bleUUID;

    // ble相关comand
    private DiscoverDevices discoverCommand;
    private ConnectToDevice connectCommand;
    private ReadFromCharacteristic readFromCharacteristic;

    void Start()
    {
        // 默认10秒超时
        discoverCommand = new DiscoverDevices(OnDeviceFound);
        if (!Permission.HasUserAuthorizedPermission(blePermissions[0]))
        {
            var callbacks = new PermissionCallbacks();
            callbacks.PermissionDenied += PermissionCallbacks_PermissionDenied;
            callbacks.PermissionGranted += PermissionCallbacks_PermissionGranted;
            callbacks.PermissionDeniedAndDontAskAgain += PermissionCallbacks_PermissionDeniedAndDontAskAgain;
            Permission.RequestUserPermissions(blePermissions, callbacks);
        }
        else
        {
            grantedCount = blePermissions.Length;
            BleGranted();
        }
    }
    internal void PermissionCallbacks_PermissionDeniedAndDontAskAgain(string permissionName)
    {
        AndroidToast.ToastStringShow("获取权限失败:"+ permissionName);
    }

    internal void PermissionCallbacks_PermissionGranted(string permissionName)
    {
        BleGranted();
    }

    internal void PermissionCallbacks_PermissionDenied(string permissionName)
    {
        AndroidToast.ToastStringShow("获取权限失败:" + permissionName);
    }

    private void BleGranted()
    {
        if ((++grantedCount) >= blePermissions.Length)
        {
            BleManager.Instance.QueueCommand(discoverCommand);
            AndroidToast.ToastStringShow("开始扫描");
        }
    }

    // 设备发现回调。name是uuid, device是名称(???)
    private void OnDeviceFound(string name, string device)
    {
        if (!string.IsNullOrEmpty(device) && device.StartsWith(bleKey))
        {
            // 已找到对应的设备，停止扫描
            discoverCommand.EndOnTimeout();
            Debug.Log($"找到设备name:{name}, device:{device}");
            // 开始连接设备
            bleUUID = name;
            connectCommand = new ConnectToDevice(bleUUID, OnConnected);
            BleManager.Instance.QueueCommand(connectCommand);
            AndroidToast.ToastStringShow("连接设备");
        }
    }

    // 设备连接回调
    private void OnConnected(string deviceUuid)
    {
        //Replace these Characteristics with YOUR device's characteristics
        readFromCharacteristic = new ReadFromCharacteristic(bleUUID, "6E400001-B5A3-F393-E0A9-E50E24DCCA9E", "6E400002-B5A3-F393-E0A9-E50E24DCCA9E", (byte[] value) =>
        {
            Debug.Log("ReadFromCharacteristic:" + Encoding.UTF8.GetString(value));
        }, true);
        BleManager.Instance.QueueCommand(readFromCharacteristic);
    }

    private void Update()
    {
        // 游戏退出按钮事件
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            Application.Quit();
        }
    }
}
