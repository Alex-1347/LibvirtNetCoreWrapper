Imports System
Imports Libvirt
'https://github.com/IDNT/AppBasics-Virtualization-Libvirt
Module Program
    Sub Main(args As String())

        Dim KvmHost As LibvirtConnection

        Console.Write("Get password string" & vbCrLf & ">")
        Dim TmpPass = ReadPassword()
        Console.WriteLine()
        Dim LibVirtAU As LibvirtAuthentication = New OpenAuthPasswordAuth With {.Username = "root", .Password = DecryptString(My.Resources.Pass, TmpPass)}
        Try
            KvmHost = LibvirtConnection.Create.WithCredentials(LibVirtAU).Connect(My.Resources.Url, LibVirtAU)
        Catch ex As Exception
            Console.WriteLine(ex.Message)
        End Try

        Console.WriteLine()
        Console.WriteLine($"Connected to node {KvmHost.Node.Hostname}")
        Console.WriteLine(Environment.NewLine + "[Node]")
        Console.WriteLine($"   Total Memory ..........: {KvmHost.Node.MemoryKBytes / 1024 / 1024} GB")
        Console.WriteLine($"   Free Memory ...........: {KvmHost.Node.MemFreeBytes / 1024 / 1024 / 1024} GB")
        Console.WriteLine($"   CPU Model .............: {KvmHost.Node.CpuModelName}")
        Console.WriteLine($"   CPU Frequency .........: {KvmHost.Node.CpuFrequencyMhz} MHz")
        Console.WriteLine($"   CPU NUMA Nodes ........: {KvmHost.Node.CpuNumaNodes}")
        Console.WriteLine($"   CPU Sockets per Node ..: {KvmHost.Node.CpuSocketsPerNode}")
        Console.WriteLine($"   CPU Cores per Socket ..: {KvmHost.Node.CpuCoresPerSocket}")
        Console.WriteLine($"   CPU Threads per Core ..: {KvmHost.Node.CpuThreadsPerCore}")

        Console.WriteLine("[VMS]")
        For Each VM In KvmHost.Domains
            'full VM.XmlDescription working!
            Console.WriteLine($"   {VM.Name} ({VM.UniqueId}) {VM.State} osInfo={VM.OsInfoId} Up={TimeSpan.FromSeconds(VM.UptimeSeconds).ToString()}")
            Console.WriteLine($"    memory={VM.MemoryMaxKbyte / 1024} MB")

            Dim FS1 = New IO.FileStream($"{VM.Name}.jpg", IO.FileMode.Create)
            VM.GetScreenshot(FS1, System.Drawing.Imaging.ImageFormat.Jpeg)
            FS1.Close()
            IO.File.WriteAllText($"{VM.Name}.xml", VM.XmlDescription.OuterXml)

            Console.WriteLine("      [Nic]")
            Try
                For Each Nic In VM.NetworkInterfaces
                    Console.WriteLine($"         {Nic.Address.ToString()} bridge={Nic.Source.Network}, mac={Nic.MAC.Address}")
                Next
            Catch ex As Exception
                Console.WriteLine(ex.Message)
            End Try

            GoTo SkipError1
            Console.WriteLine("      [Disks]")
            'VM.DiskDevices dont working => InvalidOperationException: Instance validation error: 'block' is not a valid value for VirXmlDomainDiskType.
            Try
                For Each dev In VM.DiskDevices.ToList
                    Console.WriteLine($"         {dev.Address.ToString()} {dev.Device} (driver={dev.Driver}) target={dev.Target.ToString()} source={dev.Source?.GetPath()}")
                    Console.WriteLine()
                Next
            Catch ex As Exception
                Console.WriteLine(ex.Message)
            End Try
SkipError1:
        Next

        GoTo SkipError2
        'dont working => Can't determine path of storage pool $dsk-c with driver disk.
        Console.WriteLine()
        Console.WriteLine("[KvmHost Pools]")
        Try
            For Each Pool In KvmHost.StoragePools.ToList
                Console.WriteLine($"   {Pool.Name} (state={Pool.State} driver={Pool.DriverType} path={Pool.GetPath()}) {Pool.CapacityInByte / 1024 / 1024 / 1024} GB ({Pool.ByteAvailable / 1024 / 1024 / 1024} GB free)")
            Next
        Catch ex As Exception
            Console.WriteLine(ex.Message)
        End Try
SkipError2:

        Console.WriteLine()
        Console.WriteLine("[KvmHost Volumes]")
        Try
            For Each Volume In KvmHost.StorageVolumes
                Console.WriteLine($"      Volume {Volume.Name} (type={Volume.VolumeType}, path={Volume.Path}) {Volume.CapacityInByte / 1024 / 1024 / 1024} GB ({Volume.ByteAllocated / 1024 / 1024 / 1024} GB allocated)")
            Next
        Catch ex As Exception
            Console.WriteLine(ex.Message)
        End Try

        Console.WriteLine("press [ENTER] to exit CPU Utilization")
        Dim VM1 = KvmHost.Domains.Where(Function(X) X.Name = "D82-site").FirstOrDefault
        While Not Console.KeyAvailable
            Console.WriteLine($"{VM1.Name}'s CPU Utilization = {VM1.CpuUtilization.LastSecond}%")
            Threading.Thread.Sleep(1000)
        End While


        Console.WriteLine()
        Console.WriteLine("Waiting for VM lifecycle events...")
        AddHandler KvmHost.DomainEventReceived, AddressOf KvmHost_DomainEventReceived
        Console.WriteLine()
        Console.WriteLine("[ENTER] to exit")
        Console.ReadLine()
        KvmHost.Close()

    End Sub
    Private Sub KvmHost_DomainEventReceived(ByVal sender As Object, ByVal e As VirDomainEventArgs)
        Dim VM = CType(sender, LibvirtDomain)
        Console.WriteLine($"EVENT: {e.UniqueId} {VM?.Name} {e.EventType}")
    End Sub


#Region "console"
    Function ReadPassword() As String
        Dim Pass1 As New Text.StringBuilder
        While (True)
            Dim OneKey As ConsoleKeyInfo = Console.ReadKey(True)
            Select Case OneKey.Key
                Case = ConsoleKey.Enter
                    Return Pass1.ToString
                Case ConsoleKey.Backspace
                    If Pass1.Length > 1 Then
                        Pass1.Remove(Pass1.Length - 1, 1)
                        Console.Write(vbBack)
                    End If

                Case Else
                    If Not Char.IsControl(OneKey.KeyChar) Then
                        Pass1.Append(OneKey.KeyChar)
                        Console.Write("*")
                    End If
            End Select
        End While
    End Function
#End Region

End Module


