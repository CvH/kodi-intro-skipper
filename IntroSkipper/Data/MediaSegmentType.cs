// SPDX-FileCopyrightText: 2026 CvH
// SPDX-License-Identifier: GPL-3.0-only

namespace IntroSkipper.Data;

/// <summary>
/// Classifies media segment types.
/// </summary>
public enum MediaSegmentType
{
    /// <summary>Unknown segment.</summary>
    Unknown = 0,

    /// <summary>Introduction sequence.</summary>
    Intro = 1,

    /// <summary>Credits / outro sequence.</summary>
    Outro = 2,

    /// <summary>Preview sequence.</summary>
    Preview = 3,

    /// <summary>Recap sequence.</summary>
    Recap = 4,

    /// <summary>Commercial break.</summary>
    Commercial = 5
}
