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
    public void GetHtml_ContainsAlpineScriptAndDirectives()
    {
        var html = DashboardPage.GetHtml();

        // Alpine script reference & cloak style
        Assert.Contains("<script defer src=\"/js/alpine.min.js\"></script>", html, StringComparison.Ordinal);
        Assert.Contains("[x-cloak] { display: none !important; }", html, StringComparison.Ordinal);

        // Root component
        Assert.Contains("x-data=\"dashboardApp()\"", html, StringComparison.Ordinal);
        Assert.Contains("x-cloak", html, StringComparison.Ordinal);

        // Core component function
        Assert.Contains("function dashboardApp()", html, StringComparison.Ordinal);

        // Reactive bindings for tabs & save bar
        Assert.Contains(":class=\"{ 'active': activeTab === 'tab-overview' }\"", html, StringComparison.Ordinal);
        Assert.Contains(":class=\"{ 'active': activeTab === 'tab-kodi' }\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"saveBar\" class=\"save-bar\" :class=\"{ 'visible': isDirty }\" x-show=\"isDirty\"", html, StringComparison.Ordinal);

        // Settings models
        Assert.Contains("x-model=\"config.kodiIntroAction\"", html, StringComparison.Ordinal);
        Assert.Contains("x-model.number=\"config.analysisPercent\"", html, StringComparison.Ordinal);
        Assert.Contains("x-model.number=\"config.settledSeasonDelayHours\"", html, StringComparison.Ordinal);

        // Media sources template & modal
        Assert.Contains("<template x-for=\"src in sortedSources\"", html, StringComparison.Ordinal);
        Assert.Contains("get sortedSources()", html, StringComparison.Ordinal);
        Assert.Contains("id=\"confirmSourceModal\"", html, StringComparison.Ordinal);
        Assert.Contains("x-show=\"confirmModal.open\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"toast\" class=\"toast\" :class=\"{ 'show': toast.show", html, StringComparison.Ordinal);

        // Alpine component registration & script string integrity
        Assert.Contains("Alpine.data('dashboardApp', dashboardApp)", html, StringComparison.Ordinal);
        Assert.Contains("!confirm(`Remove scan source:\\n${path}?`)", html, StringComparison.Ordinal);
        Assert.DoesNotContain("'Remove scan source:\n", html, StringComparison.Ordinal);
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
