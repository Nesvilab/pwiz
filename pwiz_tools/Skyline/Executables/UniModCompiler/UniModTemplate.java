package com.dmtavt.fragpipe.tools.skyline;

import com.dmtavt.fragpipe.tools.skyline.UniModModificationData.LabelAtoms;

public class UniModData {

  public static UniModModificationData DEFAULT = new UniModModificationData(
      "Carbamidomethyl (C)",
      "C", LabelAtoms.None, "H3C2NO", 4, 57.021464,
      true, "CAM", false);

  public static UniModModificationData[] UNI_MOD_DATA = {
      // ADD MODS.
  };

  public static UniModModificationData[] SKYLINE_HARDCODED_MOD = {
        // Hardcoded Skyline Mods
        new UniModModificationData(
            "Ammonia Loss (K, N, Q, R)",
            "K, N, Q, R", LabelAtoms.None, new String[]{"NH3"}, -17.026549,
            true, false
        ),
        new UniModModificationData(
            "Water Loss (D, E, S, T)",
            "D, E, S, T", LabelAtoms.None, new String[]{"H2O"}, -18.010565,
            true, false
        ),
        new UniModModificationData(
            "Label:15N",
            LabelAtoms.N15, 1.0033548378,
            false, false
        ),
        new UniModModificationData(
            "Label:13C",
            LabelAtoms.C13, 1.0033548378,
            false, false
        ),
        new UniModModificationData(
            "Label:13C15N",
            LabelAtoms.C13N15, 2.0067096756,
            false, false
        ),
        new UniModModificationData(
            "Label:13C(6)15N(2) (C-term K)",
            "K", 'C', LabelAtoms.C13N15, 8.014199,
            false, false
        ),
        new UniModModificationData(
            "Label:13C(6)15N(4) (C-term R)",
            "R", 'C', LabelAtoms.C13N15, 10.008269,
            false, false
        ),
        new UniModModificationData(
            "Label:13C(6) (C-term K)",
            "K", 'C', LabelAtoms.C13, 6.020129,
            false, false
        ),
        new UniModModificationData(
            "Label:13C(6) (C-term R)",
            "R", 'C', LabelAtoms.C13, 6.020129,
            false, false
        )
  };
}
