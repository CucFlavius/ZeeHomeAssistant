using System.Runtime.InteropServices;
using System.Text;
using WCharT;

namespace HidApi;

/// <summary>
/// Describes a HID device.
/// </summary>
/// <param name="Path">Path of the device</param>
/// <param name="VendorId">Vendor id</param>
/// <param name="ProductId">Product id</param>
/// <param name="SerialNumber">Serial Number</param>
/// <param name="ReleaseNumber">Release number</param>
/// <param name="ManufacturerString">Manufacturer string</param>
/// <param name="ProductString">Product string</param>
/// <param name="UsagePage">Usage page</param>
/// <param name="Usage">Usage</param>
/// <param name="InterfaceNumber">interface number</param>
/// <param name="BusType"><see cref="BusType"/> (Available since hidapi 0.13.0)</param>
public record DeviceInfo(
    string Path
    , ushort VendorId
    , ushort ProductId
    , string SerialNumber
    , ushort ReleaseNumber
    , string ManufacturerString
    , string ProductString
    , ushort UsagePage
    , ushort Usage
    , int InterfaceNumber
    , BusType BusType
)
{
    /// <summary>
    /// Connects to the device defined by the 'Path' property.
    /// </summary>
    /// <returns>A new <see cref="Device"/></returns>
    public Device ConnectToDevice()
    {
        return new Device(Path);
    }

    internal static unsafe DeviceInfo From(NativeDeviceInfo* nativeDeviceInfo)
    {
        return new DeviceInfo(
            Path: Marshal.PtrToStringAnsi((IntPtr) nativeDeviceInfo->Path) ?? string.Empty
            , VendorId: nativeDeviceInfo->VendorId
            , ProductId: nativeDeviceInfo->ProductId
            , SerialNumber: new WCharTString(nativeDeviceInfo->SerialNumber).GetString()
            , ReleaseNumber: nativeDeviceInfo->ReleaseNumber
            , ManufacturerString: new WCharTString(nativeDeviceInfo->ManufacturerString).GetString()
            , ProductString: new WCharTString(nativeDeviceInfo->ProductString).GetString()
            , UsagePage: nativeDeviceInfo->UsagePage
            , Usage: nativeDeviceInfo->Usage
            , InterfaceNumber: nativeDeviceInfo->InterfaceNumber
            , BusType: nativeDeviceInfo->BusType
        );
    }

    public sealed override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Path: {Path}");
        sb.AppendLine($"VendorId: {VendorId}");
        sb.AppendLine($"ProductId: {ProductId}");
        sb.AppendLine($"SerialNumber: {SerialNumber}");
        sb.AppendLine($"ReleaseNumber: {ReleaseNumber}");
        sb.AppendLine($"ManufacturerString: {ManufacturerString}");
        sb.AppendLine($"ProductString: {ProductString}");
        sb.AppendLine($"UsagePage: {UsagePage}");
        sb.AppendLine($"Usage: {Usage}");
        sb.AppendLine($"InterfaceNumber: {InterfaceNumber}");
        sb.AppendLine($"BusType: {BusType}");
        return sb.ToString();
    }
}
