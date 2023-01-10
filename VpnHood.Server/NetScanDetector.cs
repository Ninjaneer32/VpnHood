﻿using System;
using System.Net;
using System.Net.Sockets;
using VpnHood.Common.Collections;

namespace VpnHood.Server;

public class NetScanDetector
{
    private readonly int _itemLimit;
    private readonly TimeoutDictionary<IPAddress, ParentIpAddressItem> _parentEndPoints;

    public NetScanDetector(int itemLimit, TimeSpan itemTimeout)
    {
        _itemLimit = itemLimit;
        _parentEndPoints = new TimeoutDictionary<IPAddress, ParentIpAddressItem>(itemTimeout);
    }

    private IPAddress GetParentIpAddress(IPEndPoint ipEndPoint)
    {
        var bytes = ipEndPoint.Address.GetAddressBytes();
        bytes[3] = 0;
        return new IPAddress(bytes);
    }

    public bool Verify(IPEndPoint ipEndPoint)
    {
        if (ipEndPoint.AddressFamily == AddressFamily.InterNetworkV6)
            return true;

        var item = _parentEndPoints.GetOrAdd(GetParentIpAddress(ipEndPoint),
            _ => new ParentIpAddressItem(_parentEndPoints.Timeout));
        item.EndPoints.GetOrAdd(ipEndPoint, _ => new TimeoutItem());

        if (item.EndPoints.Count <= _itemLimit)
            return true;

        item.EndPoints.Cleanup(true);
        return item.EndPoints.Count < _itemLimit;
    }

    private class ParentIpAddressItem : TimeoutItem
    {
        public TimeoutDictionary<IPEndPoint, TimeoutItem> EndPoints { get; }

        public ParentIpAddressItem(TimeSpan? timeout)
        {
            EndPoints = new TimeoutDictionary<IPEndPoint, TimeoutItem>(timeout);
        }
    }

    public int GetBurstCount(IPEndPoint ipEndPoint)
    {
        return _parentEndPoints.TryGetValue(GetParentIpAddress(ipEndPoint), out var value)
            ? value.EndPoints.Count : 0;
    }
}