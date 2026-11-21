using System;
using WhatsLAN.Core.Utils;
using Xunit;

namespace WhatsLAN.Tests;

public class DateTimeFormatterTests
{
    [Fact]
    public void FormatChatListTime_Today_ReturnsHHmm()
    {
        var now = new DateTime(2026, 11, 21, 14, 30, 0, DateTimeKind.Utc);
        var messageTime = now.AddMinutes(-5); // same day
        var expectedTime = messageTime.ToLocalTime().ToString("HH:mm");

        var result = DateTimeFormatter.FormatChatListTime(messageTime, now);

        Assert.Equal(expectedTime, result);
    }

    [Fact]
    public void FormatChatListTime_Yesterday_ReturnsHier()
    {
        var now = new DateTime(2026, 11, 21, 14, 30, 0, DateTimeKind.Utc);
        var messageTime = now.AddDays(-1);

        var result = DateTimeFormatter.FormatChatListTime(messageTime, now);

        Assert.Equal("Hier", result);
    }

    [Fact]
    public void FormatChatListTime_WithinSevenDays_ReturnsDayName()
    {
        // 2026-11-21 is a Saturday. 3 days before is Wednesday (mercredi)
        var now = new DateTime(2026, 11, 21, 14, 30, 0, DateTimeKind.Utc);
        var messageTime = now.AddDays(-3);

        var result = DateTimeFormatter.FormatChatListTime(messageTime, now);

        Assert.Equal("mercredi", result.ToLowerInvariant());
    }

    [Fact]
    public void FormatChatListTime_OlderThanSevenDays_ReturnsDate()
    {
        var now = new DateTime(2026, 11, 21, 14, 30, 0, DateTimeKind.Utc);
        var messageTime = now.AddDays(-10);

        var result = DateTimeFormatter.FormatChatListTime(messageTime, now);

        Assert.Equal(messageTime.ToLocalTime().ToString("dd/MM/yyyy"), result);
    }

    [Fact]
    public void FormatDateSeparator_Today_ReturnsAUJOURDHUI()
    {
        var now = new DateTime(2026, 11, 21, 10, 0, 0, DateTimeKind.Utc);
        var result = DateTimeFormatter.FormatDateSeparator(now, now);

        Assert.Equal("AUJOURD'HUI", result);
    }

    [Fact]
    public void FormatDateSeparator_Yesterday_ReturnsHIER()
    {
        var now = new DateTime(2026, 11, 21, 10, 0, 0, DateTimeKind.Utc);
        var yesterday = now.AddDays(-1);
        var result = DateTimeFormatter.FormatDateSeparator(yesterday, now);

        Assert.Equal("HIER", result);
    }

    [Fact]
    public void FormatLastSeen_Online_ReturnsEnLigne()
    {
        var result = DateTimeFormatter.FormatLastSeen(isOnline: true, lastSeenUtc: null);
        Assert.Equal("en ligne", result);
    }

    [Fact]
    public void FormatLastSeen_OfflineNull_ReturnsHorsLigne()
    {
        var result = DateTimeFormatter.FormatLastSeen(isOnline: false, lastSeenUtc: null);
        Assert.Equal("hors ligne", result);
    }

    [Fact]
    public void FormatLastSeen_OfflineToday_ReturnsVuAujourdhui()
    {
        var now = new DateTime(2026, 11, 21, 14, 30, 0, DateTimeKind.Utc);
        var lastSeen = now.AddMinutes(-30);
        var expectedTime = lastSeen.ToLocalTime().ToString("HH:mm");

        var result = DateTimeFormatter.FormatLastSeen(isOnline: false, lastSeenUtc: lastSeen, relativeNow: now);

        Assert.Equal($"vu aujourd’hui à {expectedTime}", result);
    }

    [Fact]
    public void FormatLastSeen_OfflineYesterday_ReturnsVuHier()
    {
        var now = new DateTime(2026, 11, 21, 14, 30, 0, DateTimeKind.Utc);
        var lastSeen = now.AddDays(-1);
        var expectedTime = lastSeen.ToLocalTime().ToString("HH:mm");

        var result = DateTimeFormatter.FormatLastSeen(isOnline: false, lastSeenUtc: lastSeen, relativeNow: now);

        Assert.Equal($"vu hier à {expectedTime}", result);
    }
}
