// SPDX-FileCopyrightText: 2026 CvH
// SPDX-License-Identifier: GPL-3.0-only

using System;
using IntroSkipper.Server.Web;
using Xunit;

namespace IntroSkipper.Tests;

/// <summary>
/// Unit tests for <see cref="DashboardPage"/> and its embedded assets.
/// </summary>
public sealed class TestDashboardPage
{
    [Fact]
    public void GetHtml_ReturnsNonEmptyHtmlWithExpectedTitle()
    {
        var html = DashboardPage.GetHtml();

        Assert.False(string.IsNullOrWhiteSpace(html));
        Assert.Contains("<title>IntroSkipper Dashboard</title>", html, StringComparison.Ordinal);
        Assert.Contains("<!DOCTYPE html>", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetFontBytes_ReturnsNonEmptyInterWoff2Binary()
    {
        var fontBytes = DashboardPage.GetFontBytes();

        Assert.NotNull(fontBytes);
        Assert.NotEmpty(fontBytes);
        // WOFF2 magic header: 0x77 0x4F 0x46 0x32 ('wOF2')
        Assert.True(fontBytes.Length > 4);
        Assert.Equal((byte)'w', fontBytes[0]);
        Assert.Equal((byte)'O', fontBytes[1]);
        Assert.Equal((byte)'F', fontBytes[2]);
        Assert.Equal((byte)'2', fontBytes[3]);
    }

    [Fact]
    public void GetAlpineBytes_ReturnsNonEmptyAlpineScriptBinary()
    {
        var alpineBytes = DashboardPage.GetAlpineBytes();

        Assert.NotNull(alpineBytes);
        Assert.NotEmpty(alpineBytes);
        Assert.True(alpineBytes.Length > 1000);
    }
}
