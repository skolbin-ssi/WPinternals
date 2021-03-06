﻿// Copyright (c) 2018, Rene Lergner - wpinternals.net - @Heathcliff74xda
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using MadWizard.WinUSBNet;
using System;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WPinternals
{
    internal delegate void NewDeviceArrivedEvent(ArrivalEventArgs Args);
    internal delegate void DeviceRemovedEvent();

    internal class PhoneNotifierViewModel
    {
        private USBNotifier LumiaOldCombiNotifier;
        private USBNotifier LumiaNewCombiNotifier;
        private USBNotifier LumiaNormalNotifier;
        private USBNotifier LumiaFlashNotifier;
        private USBNotifier StorageNotifier;
        private USBNotifier ComPortNotifier;
        private USBNotifier LumiaEmergencyNotifier;
        private USBNotifier LumiaLabelNotifier;
        private USBNotifier HidInterfaceNotifier;

        public PhoneInterfaces? CurrentInterface = null;
        private PhoneInterfaces? LastInterface = null;
        public IDisposable CurrentModel = null;

        public event NewDeviceArrivedEvent NewDeviceArrived = delegate { };
        public event DeviceRemovedEvent DeviceRemoved = delegate { };


        private Guid OldCombiInterfaceGuid = new Guid("{0FD3B15C-D457-45d8-A779-C2B2C9F9D0FD}");
        private Guid NewCombiInterfaceGuid = new Guid("{7eaff726-34cc-4204-b09d-f95471b873cf}");

        private Guid MassStorageInterfaceGuid = new Guid("{53F56307-B6BF-11D0-94F2-00A0C91EFB8B}");
        private Guid ComPortInterfaceGuid = new Guid("{86E0D1E0-8089-11D0-9CE4-08003E301F73}");
        private Guid HidInterfaceGuid = new Guid("{4D1E55B2-F16F-11CF-88CB-001111000030}");

        private Guid LumiaNormalInterfaceGuid = new Guid("{08324F9C-B621-435C-859B-AE4652481B7C}");
        private Guid LumiaLabelInterfaceGuid = new Guid("{F4FE0C27-7304-4ED7-AAB5-130893B84B6F}");
        private Guid LumiaFlashInterfaceGuid = new Guid("{9e3bd5f7-9690-4fcc-8810-3e2650cd6ecc}");
        private Guid LumiaEmergencyInterfaceGuid = new Guid("{71DE994D-8B7C-43DB-A27E-2AE7CD579A0C}");


        private object ModelLock = new object();

        private EventWaitHandle NewInterfaceWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);

        private EventLogWatcher LogWatcher;

        private string Qcom9006DevicePath;

        internal void Start()
        {
            LumiaOldCombiNotifier = new USBNotifier(OldCombiInterfaceGuid);
            LumiaOldCombiNotifier.Arrival += LumiaNotifier_Arrival;
            LumiaOldCombiNotifier.Removal += LumiaNotifier_Removal;

            LumiaNewCombiNotifier = new USBNotifier(NewCombiInterfaceGuid);
            LumiaNewCombiNotifier.Arrival += LumiaNotifier_Arrival;
            LumiaNewCombiNotifier.Removal += LumiaNotifier_Removal;

            LumiaNormalNotifier = new USBNotifier(LumiaNormalInterfaceGuid);
            LumiaNormalNotifier.Arrival += LumiaNotifier_Arrival;
            LumiaNormalNotifier.Removal += LumiaNotifier_Removal;

            LumiaFlashNotifier = new USBNotifier(LumiaFlashInterfaceGuid);
            LumiaFlashNotifier.Arrival += LumiaNotifier_Arrival;
            LumiaFlashNotifier.Removal += LumiaNotifier_Removal;

            StorageNotifier = new USBNotifier(MassStorageInterfaceGuid);
            StorageNotifier.Arrival += LumiaNotifier_Arrival;
            StorageNotifier.Removal += LumiaNotifier_Removal;

            ComPortNotifier = new USBNotifier(ComPortInterfaceGuid);
            ComPortNotifier.Arrival += LumiaNotifier_Arrival;
            ComPortNotifier.Removal += LumiaNotifier_Removal;

            LumiaEmergencyNotifier = new USBNotifier(LumiaEmergencyInterfaceGuid);
            LumiaEmergencyNotifier.Arrival += LumiaNotifier_Arrival;
            LumiaEmergencyNotifier.Removal += LumiaNotifier_Removal;

            LumiaLabelNotifier = new USBNotifier(LumiaLabelInterfaceGuid);
            LumiaLabelNotifier.Arrival += LumiaNotifier_Arrival;
            LumiaLabelNotifier.Removal += LumiaNotifier_Removal;

            HidInterfaceNotifier = new USBNotifier(HidInterfaceGuid);
            HidInterfaceNotifier.Arrival += LumiaNotifier_Arrival;
            HidInterfaceNotifier.Removal += LumiaNotifier_Removal;

            try
            {
                EventLogQuery LogQuery = new EventLogQuery("Microsoft-Windows-Kernel-PnP/Configuration", PathType.LogName, "*[System[(EventID = 411)]]");
                LogWatcher = new EventLogWatcher(LogQuery);
                LogWatcher.EventRecordWritten += new EventHandler<EventRecordWrittenEventArgs>(PnPEventWritten);
                LogWatcher.Enabled = true;
                App.IsPnPEventLogMissing = false;
            }
            catch { }
        }

        private void PnPEventWritten(Object obj, EventRecordWrittenEventArgs arg)
        {
            string Description = arg.EventRecord.FormatDescription();
            if (Description.IndexOf("VID_045E&PID_9006", 0, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                LogFile.Log("Event " + arg.EventRecord.Id.ToString() + ": " + Description, LogType.FileOnly);
                LogFile.Log("Phone switched to Mass Storage mode, but the driver on the PC did not start correctly", LogType.FileAndConsole);
                CurrentInterface = PhoneInterfaces.Lumia_BadMassStorage;
                CurrentModel = null;
                NewDeviceArrived(new ArrivalEventArgs((PhoneInterfaces)CurrentInterface, CurrentModel));
            }
        }

        internal void Stop()
        {
            LumiaOldCombiNotifier.Dispose();
            LumiaNewCombiNotifier.Dispose();
            LumiaNormalNotifier.Dispose();
            LumiaFlashNotifier.Dispose();
            StorageNotifier.Dispose();
            ComPortNotifier.Dispose();
            LumiaEmergencyNotifier.Dispose();
            HidInterfaceNotifier.Dispose();
            LogWatcher.Dispose();
        }

        internal async Task WaitForNextNodeChange()
        {
            // Node change events are on all USBnotifiers, so we just pick one
            await LumiaEmergencyNotifier.WaitForNextNodeChange();
        }

        internal void NotifyArrival()
        {
            if (CurrentInterface != null)
                NewDeviceArrived(new ArrivalEventArgs((PhoneInterfaces)CurrentInterface, CurrentModel));
        }

        void LumiaNotifier_Arrival(object sender, USBEvent e)
        {
            try
            {
                if ((e.DevicePath.IndexOf("VID_0421&", StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (e.DevicePath.IndexOf("VID_045E&", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    if ((e.DevicePath.IndexOf("&PID_0660&MI_04", StringComparison.OrdinalIgnoreCase) >= 0) ||
                        (e.DevicePath.IndexOf("&PID_0713&MI_04", StringComparison.OrdinalIgnoreCase) >= 0) || // for Spec B
                        (e.DevicePath.IndexOf("&PID_0A01&MI_04", StringComparison.OrdinalIgnoreCase) >= 0)) // for Spec B (650)
                    {
                        CurrentInterface = PhoneInterfaces.Lumia_Label;
                        CurrentModel = new NokiaPhoneModel(e.DevicePath);
                        LogFile.Log("Found device on interface: " + ((USBNotifier)sender).Guid.ToString(), LogType.FileOnly);
                        LogFile.Log("Device path: " + e.DevicePath, LogType.FileOnly);
                        LogFile.Log("Connected device: Lumia", LogType.FileAndConsole);
                        LogFile.Log("Mode: Label", LogType.FileAndConsole);
                        NewDeviceArrived(new ArrivalEventArgs((PhoneInterfaces)CurrentInterface, CurrentModel));
                    }
                    else if ((e.DevicePath.IndexOf("&PID_0661", StringComparison.OrdinalIgnoreCase) >= 0) ||
                             (e.DevicePath.IndexOf("&PID_06FC", StringComparison.OrdinalIgnoreCase) >= 0) || // VID_0421&PID_06FC is for Lumia 930
                             (e.DevicePath.IndexOf("&PID_0A00", StringComparison.OrdinalIgnoreCase) >= 0))   // vid_045e & pid_0a00 & mi_03 = Lumia 950 XL normal mode
                    {
                        if (((USBNotifier)sender).Guid == OldCombiInterfaceGuid)
                        {
                            NewInterfaceWaitHandle.Reset();
                            if (USBDevice.GetDevices(NewCombiInterfaceGuid).Count() > 0)
                                return;
                            else
                            {
                                // Old combi-interface was detected, but new combi-interface was not detected.
                                // This could mean 2 things:
                                // - It is a WP80 phone, which has only this old combi-interface to talk to.
                                // - It is a WP81 / W10M phone, which has an unresponsive old combi-interface and we need to wait for the new combi-interface to arrive.
                                // We will wait maximum 1 sec for the new interface. If it doesn't arrive we will start talking on this old interface.
                                // We will start a new thread, because if this thread is blocked, no new devices will arrive.
                                string DevicePath = e.DevicePath;
                                ThreadPool.QueueUserWorkItem(s =>
                                {
                                    if (!NewInterfaceWaitHandle.WaitOne(1000))
                                    {
                                        // Waithandle not set.
                                        // So new interface did not arrive.
                                        // So we assume we need to talk to this old interface.

                                        CurrentInterface = PhoneInterfaces.Lumia_Normal;
                                        CurrentModel = new NokiaPhoneModel(DevicePath);
                                        LogFile.Log("Found device on interface: " + ((USBNotifier)sender).Guid.ToString(), LogType.FileOnly);
                                        LogFile.Log("Device path: " + e.DevicePath, LogType.FileOnly);
                                        LogFile.Log("Connected device: Lumia", LogType.FileAndConsole);
                                        LogFile.Log("Mode: Normal", LogType.FileAndConsole);
                                        NewDeviceArrived(new ArrivalEventArgs((PhoneInterfaces)CurrentInterface, CurrentModel));
                                    }
                                });
                            }
                        }
                        else
                        {
                            NewInterfaceWaitHandle.Set();

                            CurrentInterface = PhoneInterfaces.Lumia_Normal;
                            CurrentModel = new NokiaPhoneModel(e.DevicePath);
                            LogFile.Log("Found device on interface: " + ((USBNotifier)sender).Guid.ToString(), LogType.FileOnly);
                            LogFile.Log("Device path: " + e.DevicePath, LogType.FileOnly);
                            LogFile.Log("Connected device: Lumia", LogType.FileAndConsole);
                            LogFile.Log("Mode: Normal", LogType.FileAndConsole);
                            NewDeviceArrived(new ArrivalEventArgs((PhoneInterfaces)CurrentInterface, CurrentModel));
                        }
                    }
                    else if ((e.DevicePath.IndexOf("&PID_066E", StringComparison.OrdinalIgnoreCase) >= 0) ||
                             (e.DevicePath.IndexOf("&PID_0714", StringComparison.OrdinalIgnoreCase) >= 0) || // VID_0421&PID_0714 is for Lumia 930
                             (e.DevicePath.IndexOf("&PID_0A02", StringComparison.OrdinalIgnoreCase) >= 0) || // VID_045E&PID_0A02 is for Lumia 950
                             (e.DevicePath.IndexOf("&PID_05EE", StringComparison.OrdinalIgnoreCase) >= 0))   // VID_0421&PID_05EE is for early RX100
                    {
                        CurrentModel = new NokiaFlashModel(e.DevicePath);
                        ((NokiaFlashModel)CurrentModel).InterfaceChanged += InterfaceChanged;

                        FlashAppType type = FlashAppType.FlashApp;
                        try
                        {
                            type = ((NokiaFlashModel)CurrentModel).GetFlashAppType();
                            LogFile.Log("Flash App Type: " + type.ToString(), LogType.FileOnly);
                        }
                        catch
                        {
                            LogFile.Log("Flash App Type could not be determined, assuming " + type.ToString(), LogType.FileOnly);
                        }

                        switch (type)
                        {
                            case FlashAppType.BootManager:
                                {
                                    CurrentInterface = PhoneInterfaces.Lumia_Bootloader;
                                    LogFile.Log("Found device on interface: " + ((USBNotifier)sender).Guid.ToString(), LogType.FileOnly);
                                    LogFile.Log("Device path: " + e.DevicePath, LogType.FileOnly);
                                    LogFile.Log("Connected device: Lumia", LogType.FileAndConsole);
                                    LogFile.Log("Mode: Bootloader", LogType.FileAndConsole);
                                    NewDeviceArrived(new ArrivalEventArgs((PhoneInterfaces)CurrentInterface, CurrentModel));
                                    break;
                                }
                            case FlashAppType.FlashApp:
                                {
                                    ((NokiaFlashModel)CurrentModel).DisableRebootTimeOut();
                                    CurrentInterface = PhoneInterfaces.Lumia_Flash;
                                    LogFile.Log("Found device on interface: " + ((USBNotifier)sender).Guid.ToString(), LogType.FileOnly);
                                    LogFile.Log("Device path: " + e.DevicePath, LogType.FileOnly);
                                    LogFile.Log("Connected device: Lumia", LogType.FileAndConsole);
                                    LogFile.Log("Mode: Flash", LogType.FileAndConsole);
                                    NewDeviceArrived(new ArrivalEventArgs((PhoneInterfaces)CurrentInterface, CurrentModel));
                                    break;
                                }
                            case FlashAppType.PhoneInfoApp:
                                {
                                    CurrentInterface = PhoneInterfaces.Lumia_Bootloader;
                                    LogFile.Log("Found device on interface: " + ((USBNotifier)sender).Guid.ToString(), LogType.FileOnly);
                                    LogFile.Log("Device path: " + e.DevicePath, LogType.FileOnly);
                                    LogFile.Log("Connected device: Lumia", LogType.FileAndConsole);
                                    LogFile.Log("Mode: Bootloader (Phone Info)", LogType.FileAndConsole);
                                    NewDeviceArrived(new ArrivalEventArgs((PhoneInterfaces)CurrentInterface, CurrentModel));
                                    break;
                                }
                        }
                    }
                }
                else if ((e.DevicePath.IndexOf(@"DISK&VEN_QUALCOMM&PROD_MMC_STORAGE", StringComparison.OrdinalIgnoreCase) >= 0) ||
                         (e.DevicePath.IndexOf(@"DISK&VEN_MSFT&PROD_PHONE_MMC_STOR", StringComparison.OrdinalIgnoreCase) >= 0) ||
                         ((e.DevicePath.Length == @"\\.\E:".Length) && (e.DevicePath.StartsWith(@"\\.\")) && (e.DevicePath.EndsWith(":"))))
                {
#if DEBUG
                    LogFile.Log("Mass storage arrived: " + e.DevicePath, LogType.FileOnly);
                    LogFile.Log("Start new thread for getting metadata.", LogType.FileOnly);
#endif

                    // This function is possibly called by an USB notification WndProc.
                    // It is not possible to invoke COM objects from a WndProc.
                    // MassStorage uses ManagementObjectSearcher, which is a COM object.
                    // Therefore we use a new thread.
                    ThreadPool.QueueUserWorkItem(s =>
                    {
                        lock (ModelLock)
                        {
                            if (!(CurrentModel is MassStorage))
                            {
                                // Wait 1 second to make sure MainOS is loaded
                                // In case of multiple drive letters being assigned to the phone by the user
                                // MainOS may take a while to show up and we may accidentally catch up a letter that is
                                // not for MainOS.
                                Task.Delay(1000).Wait();

                                MassStorage NewModel = new MassStorage(e.DevicePath);

                                if (NewModel.Drive != null) // When logical drive is already known, we use this model. Or else we wait for the logical drive to arrive.
                                {
                                    CurrentInterface = PhoneInterfaces.Lumia_MassStorage;
                                    CurrentModel = NewModel;
                                    LogFile.Log("Found device on interface: " + ((USBNotifier)sender).Guid.ToString(), LogType.FileOnly);
                                    LogFile.Log("Device path: " + e.DevicePath, LogType.FileOnly);
                                    LogFile.Log("Connected device: Lumia", LogType.FileAndConsole);
                                    LogFile.Log("Mode: Mass storage mode", LogType.FileAndConsole);
                                    if (!string.IsNullOrEmpty(Qcom9006DevicePath))
                                    {
                                        LogFile.Log("Found 9006 device previously", LogType.FileOnly);
                                        LogFile.Log("Attaching 9006 device", LogType.FileOnly);
                                        NewModel.AttachQualcommSerial(Qcom9006DevicePath);
                                    }
                                    NewDeviceArrived(new ArrivalEventArgs((PhoneInterfaces)CurrentInterface, CurrentModel));
                                }
                            }
                        }
                    });
                }
                else if (e.DevicePath.IndexOf("VID_05C6&", StringComparison.OrdinalIgnoreCase) >= 0) // Qualcomm device
                {
                    if (e.DevicePath.IndexOf("&PID_9008", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        USBDeviceInfo DeviceInfo = USBDevice.GetDevices(((USBNotifier)sender).Guid).Where((d) => string.Compare(d.DevicePath, e.DevicePath, true) == 0).FirstOrDefault();

                        if ((DeviceInfo.BusName == "QHSUSB_DLOAD") || (DeviceInfo.BusName == "QHSUSB__BULK") || ((DeviceInfo.BusName == "") && (LastInterface != PhoneInterfaces.Qualcomm_Download))) // TODO: Separate for Sahara!
                        {
                            CurrentInterface = PhoneInterfaces.Qualcomm_Download;
                            CurrentModel = new QualcommSerial(e.DevicePath);
                            NewDeviceArrived(new ArrivalEventArgs((PhoneInterfaces)CurrentInterface, CurrentModel));
                            LogFile.Log("Found device on interface: " + ((USBNotifier)sender).Guid.ToString(), LogType.FileOnly);
                            LogFile.Log("Device path: " + e.DevicePath, LogType.FileOnly);
                            LogFile.Log("Connected device: Lumia", LogType.FileAndConsole);
                            if (DeviceInfo.BusName == "")
                                LogFile.Log("Driver does not show busname, assume mode: Qualcomm Emergency Download 9008", LogType.FileAndConsole);
                            else
                                LogFile.Log("Mode: Qualcomm Emergency Download 9008", LogType.FileAndConsole);
                        }
                        else if ((DeviceInfo.BusName == "QHSUSB_ARMPRG") || ((DeviceInfo.BusName == "") && (LastInterface == PhoneInterfaces.Qualcomm_Download)))
                        {
                            CurrentInterface = PhoneInterfaces.Qualcomm_Flash;
                            CurrentModel = new QualcommSerial(e.DevicePath);
                            NewDeviceArrived(new ArrivalEventArgs((PhoneInterfaces)CurrentInterface, CurrentModel));
                            LogFile.Log("Found device on interface: " + ((USBNotifier)sender).Guid.ToString(), LogType.FileOnly);
                            LogFile.Log("Device path: " + e.DevicePath, LogType.FileOnly);
                            LogFile.Log("Connected device: Lumia", LogType.FileAndConsole);
                            if (DeviceInfo.BusName == "")
                                LogFile.Log("Driver does not show busname, assume mode: Qualcomm Emergency Flash 9008", LogType.FileAndConsole);
                            else
                                LogFile.Log("Mode: Qualcomm Emergency Flash 9008", LogType.FileAndConsole);
                        }
                    }
                    else if (e.DevicePath.IndexOf("&PID_9006", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        // This is part of the Mass Storage inteface.
                        // It is a slightly different version of the Qualcomm Emergency interface, which is implemented in SBL3.
                        // One important difference is that the base address for sending a loader is not 0x2A000000, but it is 0x82F00000.

                        Qcom9006DevicePath = e.DevicePath;

                        LogFile.Log("Found device on interface: " + ((USBNotifier)sender).Guid.ToString(), LogType.FileOnly);
                        LogFile.Log("Device path: " + Qcom9006DevicePath, LogType.FileOnly);
                        LogFile.Log("Connected device: Lumia", LogType.FileAndConsole);
                        LogFile.Log("Mode: Qualcomm Emergency 9006", LogType.FileAndConsole);

                        if (CurrentModel is MassStorage)
                        {
                            LogFile.Log("Found Mass Storage device previously", LogType.FileOnly);
                            LogFile.Log("Attaching 9006 device", LogType.FileOnly);
                            ((MassStorage)CurrentModel).AttachQualcommSerial(Qcom9006DevicePath);
                        }
                    }
                    else if (e.DevicePath.IndexOf("&PID_F006", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        // This is part of the charging inteface.

                        LogFile.Log("Found device on interface: " + ((USBNotifier)sender).Guid.ToString(), LogType.FileOnly);
                        LogFile.Log("Device path: " + e.DevicePath, LogType.FileOnly);
                        LogFile.Log("Connected device: Lumia", LogType.FileAndConsole);
                        LogFile.Log("Mode: Qualcomm Emergency Charging F006", LogType.FileAndConsole);
                    }
                }
            }
            catch (Exception Ex)
            {
                LogFile.LogException(Ex);
                CurrentModel = null;
                CurrentInterface = null;
            }
        }

        private void InterfaceChanged(PhoneInterfaces NewInterface)
        {
            CurrentInterface = NewInterface;
        }

        void LumiaNotifier_Removal(object sender, USBEvent e)
        {
            if (e.DevicePath.IndexOf("VID_05C6&PID_9006", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Qcom9006DevicePath = null;
            }

            if (
                (e.DevicePath.IndexOf("VID_0421&PID_0660&MI_04", StringComparison.OrdinalIgnoreCase) >= 0) ||
                (e.DevicePath.IndexOf("VID_0421&PID_0713&MI_04", StringComparison.OrdinalIgnoreCase) >= 0) ||
                (e.DevicePath.IndexOf("VID_045E&PID_0A01&MI_04", StringComparison.OrdinalIgnoreCase) >= 0) ||
                (e.DevicePath.IndexOf("VID_0421&PID_0661", StringComparison.OrdinalIgnoreCase) >= 0) ||
                (e.DevicePath.IndexOf("VID_0421&PID_06FC", StringComparison.OrdinalIgnoreCase) >= 0) ||
                (e.DevicePath.IndexOf("VID_0421&PID_066E", StringComparison.OrdinalIgnoreCase) >= 0) ||
                (e.DevicePath.IndexOf("VID_0421&PID_0714", StringComparison.OrdinalIgnoreCase) >= 0) ||
                (e.DevicePath.IndexOf("VID_0421&PID_05EE", StringComparison.OrdinalIgnoreCase) >= 0) ||
                (e.DevicePath.IndexOf("VID_045E&PID_0A00", StringComparison.OrdinalIgnoreCase) >= 0) ||
                (e.DevicePath.IndexOf("VID_045E&PID_0A02", StringComparison.OrdinalIgnoreCase) >= 0) ||
                (e.DevicePath.IndexOf("VID_05C6&PID_9008", StringComparison.OrdinalIgnoreCase) >= 0) ||
                (e.DevicePath.IndexOf(@"DISK&VEN_QUALCOMM&PROD_MMC_STORAGE", StringComparison.OrdinalIgnoreCase) >= 0) ||
                (e.DevicePath.IndexOf(@"DISK&VEN_MSFT&PROD_PHONE_MMC_STOR", StringComparison.OrdinalIgnoreCase) >= 0)
            )
            {
                if (CurrentInterface != null)
                    LastInterface = CurrentInterface;
                CurrentInterface = null;
                if (CurrentModel != null)
                {
                    CurrentModel.Dispose();
                    CurrentModel = null;
                    LogFile.Log("Lumia disconnected", LogType.FileAndConsole);
                }
                DeviceRemoved();
            }
        }

        internal async Task<IDisposable> WaitForArrival()
        {
            IDisposable Result = null;

            if (CurrentInterface == null)
                LogFile.Log("Waiting for phone to connect...", LogType.FileOnly);

            await Task.Run(() =>
            {
                System.Threading.AutoResetEvent e = new System.Threading.AutoResetEvent(false);
                NewDeviceArrivedEvent Arrived = (a) =>
                    {
                        e.Set();
                        Result = a.NewModel;
                    };
                NewDeviceArrived += Arrived;
                e.WaitOne();
                NewDeviceArrived -= Arrived;
            });

            return Result;
        }

        internal async Task WaitForRemoval()
        {
            LogFile.Log("Waiting for phone to disconnect...", LogType.FileOnly);

            await Task.Run(() =>
            {
                System.Threading.AutoResetEvent e = new System.Threading.AutoResetEvent(false);
                DeviceRemovedEvent Removed = () =>
                {
                    e.Set();
                };
                DeviceRemoved += Removed;
                e.WaitOne();
                DeviceRemoved -= Removed;
            });
        }
    }
}
