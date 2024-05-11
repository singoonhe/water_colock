using Android.BLE;
using Android.BLE.Commands;
using System;
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
    public Text scanStatusText;
    public Coroutine scanCoroutine;
    public Text connectText;
    public GameObject configCell;

    void Start()
    {
        devicePanelNode.gameObject.SetActive(false);
        scanStatusText.gameObject.SetActive(false);
        // 默认10秒超时
        discoverCommand = new DiscoverDevices(OnDeviceFound, OnDeviceScanFinish);
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
            OnScanDevice();
        }
    }

    // 开始扫描事件
    public void OnScanDevice()
    {
        if (scanCoroutine != null)
        {
            AndroidToast.ToastStringShow("正在扫描中");
            return;
        }

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
        bleUUID = null;
        devicePanelNode.gameObject.SetActive(false);
        // 开始显示状态
        scanStatusText.gameObject.SetActive(true);
        scanCoroutine = StartCoroutine(ScanStatusEnumerator());
    }

    // 扫描中状态动态显示
    IEnumerator ScanStatusEnumerator()
    {
        string minText = "扫描中";
        int statusMinLen = minText.Length;
        scanStatusText.text = minText;
        while (true)
        {
            yield return new WaitForSeconds(1);
            scanStatusText.text += ".";
            if (scanStatusText.text.Length > (statusMinLen + 3))
            {
                scanStatusText.text = minText;
            }
        }
    }

    // 停止扫描状态的显示
    private void StopScanStatus()
    {
        if (scanCoroutine != null)
        {
            StopCoroutine(scanCoroutine);
            scanCoroutine = null;
        }
        scanStatusText.gameObject.SetActive(false);
    }

    // 设备发现回调。name是uuid, device是名称(???)
    private void OnDeviceFound(string name, string device)
    {
        if (!string.IsNullOrEmpty(device) && device.StartsWith(bleKey))
        {
            bleUUID = name;
            // 停止扫描状态显示
            StopScanStatus();
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

    // 设备扫描完成
    private void OnDeviceScanFinish()
    {
        StopScanStatus();
        if (string.IsNullOrEmpty(bleUUID))
            AndroidToast.ToastStringShow("扫描完成，未找到设备");
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
            if (writeCharacteristic != null)
            {
                writeCharacteristic.End();
                writeCharacteristic = null;
            }
            // 获取到的数据进行处理
            var retValue = Encoding.UTF8.GetString(value);
            Debug.Log("get msg:" + retValue);
            var configDic = JsonUtility.FromJson<Dictionary<string, string>>(retValue);
            Debug.Log(configDic);
        });
        BleManager.Instance.QueueCommand(subscribeFromCharacteristic);

        // 发送更新数据命令
        //SendData(JsonUtility.ToJson(new Dictionary<string, string>() { { "Cmd", "Get" }}));
    }

    // 发送数据
    private void SendData(string data)
    {
        string base64String = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(data));
        writeCharacteristic = new WriteToCharacteristic(bleUUID, serviceUUID, serviceWriteUUID, base64String);
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
