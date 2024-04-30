using Android.BLE;
using Android.BLE.Commands;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.UI;

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
    // 设备UUID
    private string bleUUID;
    private bool isConnected = false;
    // GATT相关UUID
    private string serviceUUID = "ffe0";
    private string serviceWriteUUID = "ffe6";
    private string serviceSubscUUID = "ffe7";

    // ble相关comand
    private DiscoverDevices discoverCommand;
    private ConnectToDevice connectCommand;
    private WriteToCharacteristic writeCharacteristic;

    // UI相关的结点绑定
    public Transform devicePanelNode;
    public Transform configPanelNode;
    public Transform selectNode;
    public Text deviceNameText;
    public Text connectText;
    public GameObject configCell;

    void Start()
    {
        devicePanelNode.gameObject.SetActive(false);
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

    // 授权完成后自动扫描
    private void BleGranted()
    {
        if ((++grantedCount) >= blePermissions.Length)
        {
            ScanDevice();
        }
    }

    // 开始扫描事件
    private void ScanDevice()
    {
        if (grantedCount >= blePermissions.Length)
        {
            BleManager.Instance.QueueCommand(discoverCommand);
            AndroidToast.ToastStringShow("开始扫描");
        }
        else
        {
            AndroidToast.ToastStringShow("请先授权");
        }
        isConnected = false;
        devicePanelNode.gameObject.SetActive(false);
    }

    // 设备发现回调。name是uuid, device是名称(???)
    private void OnDeviceFound(string name, string device)
    {
        if (!string.IsNullOrEmpty(device) && device.StartsWith(bleKey))
        {
            bleUUID = name;
            // 已找到对应的设备，停止扫描
            discoverCommand.EndOnTimeout();
            Debug.Log($"找到设备name:{name}, device:{device}");
            // 显示设备信息
            devicePanelNode.gameObject.SetActive(true);
            deviceNameText.text = name;
            connectText.text = "连接";
            // 自动连接设备
            ConnectDevice();
        }
    }

    // 开始连接设备
    public void ConnectDevice()
    {
        if (!isConnected)
        {
            foreach (Transform child in configPanelNode)
            {
                Destroy(child.gameObject);
            }
            // 开始连接设备
            connectCommand = new ConnectToDevice(bleUUID, OnConnected);
            BleManager.Instance.QueueCommand(connectCommand);
            AndroidToast.ToastStringShow("连接设备");
        }
        else
        {
            AndroidToast.ToastStringShow("已连接");
        }
    }

    // 设备连接回调
    private void OnConnected(string deviceUuid)
    {
        // 更新显示状态
        isConnected = true;
        connectText.text = "断开";

        // 注册回调方法
        var subscribeFromCharacteristic = new SubscribeToCharacteristic(bleUUID, serviceUUID, serviceSubscUUID, (byte[] value) =>
        {
            Debug.Log("subscribeFromCharacteristic:" + Encoding.UTF8.GetString(value));
            writeCharacteristic.End();
            // 获取到的数据进行处理
        });
        BleManager.Instance.QueueCommand(subscribeFromCharacteristic);

        // 发送更新数据命令
        SendData(JsonUtility.ToJson(new Dictionary<string, string>() { { "Cmd", "Get" }}));
    }

    // 发送数据
    private void SendData(string data)
    {
        writeCharacteristic = new WriteToCharacteristic(bleUUID, serviceUUID, serviceWriteUUID, data);
        BleManager.Instance.QueueCommand(writeCharacteristic);
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
