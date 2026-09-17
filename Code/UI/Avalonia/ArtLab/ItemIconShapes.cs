namespace RPGGame.UI.Avalonia.ArtLab;

/// <summary>Hand-authored component masks. M=surface, H=highlight, S=shade, G=grip, T=fitting.</summary>
internal static class ItemIconShapes
{
    // Masks are centered on a 16×16 transparent canvas; outlines are added on empty neighbor pixels only.
    public static readonly IReadOnlyDictionary<string, string[]> All = new Dictionary<string, string[]>
    {
        ["sword"] = ["..........HM", ".........HMM", "........HMM.", ".......HMM..", "......HMM...", ".....HMM....", "..T.HMM.....", "...TMM......", "...GT.......", "..GG.T......", ".GG.........", "T..........."],
        ["cleaver"] = [".........HMMM", "........HMMMS", ".......HMMMS.", "......HMMMS..", ".....HMMMS...", "..T.HMMMS....", "...TMMMS.....", "...GT........", "..GG.T.......", ".GG..........", "T............"],
        ["rapier"] = ["............H", "...........H.", "..........H..", ".........H...", "........H....", ".......H.....", "......H......", "...T.H.......", "....T........", "...GT........", "..GT.........", ".T..........."],
        ["saber"] = ["..........H.", "..........HM", ".........HMM", "........HMM.", ".......HMM..", "......HMM...", ".....HMM....", "..T.HMM.....", "...TMM......", "..GGT.......", ".GGT........", "T..........."],
        ["polearm"] = ["......HHHHMM", ".....HMMMSS.", "....HM.G....", ".......G....", "......G.....", ".....G......", "....G.......", "...G........", "..G.........", ".G..........", "G..........."],
        ["dagger"] = [".......H", "......HM", ".....HMM", "....HMM.", "...HMM..", ".T.MM...", "..TT....", ".GG.T...", "T......."],
        ["kris"] = [".......HM", ".....HMM.", "......HM.", "....HMM..", ".....HM..", "...HMM...", ".T.MM....", "..TT.....", ".GG.T....", "T........"],
        ["needle"] = ["........H", ".......HM", "......HM.", ".....HM..", "....HM...", "...HM....", ".T.M.....", "..T......", ".G.......", "T........"],
        ["mace"] = [".......H....", ".....HHMM...", "....HHMMMS..", ".....MMMS...", "......MS....", ".....GT.....", "....GG......", "...GG.......", "..GG........", ".GG.........", "T..........."],
        ["hammer"] = [".....HHHHHHM", ".....MMMMMMS", ".....SSSSSSS", ".......GT...", "......GG....", ".....GG.....", "....GG......", "...GG.......", "..GG........", ".GG.........", "T..........."],
        ["club"] = [".......HHM", "......HMMS", ".....HMMS.", "....HMMS..", "....MMS...", "....GM....", "...GG.....", "..GG......", ".GG.......", "G........."],
        ["flail"] = [".......HHM..", "......HMMMS.", ".......MMS..", "........T...", ".......T....", "......T.....", ".....G......", "....GG......", "...GG.......", "..GG........", ".GG.........", "T..........."],
        ["wand"] = ["..........H", ".........HM", "........HM.", ".......HM..", "......HM...", ".....TM....", "....GT.....", "...GG......", "..GG.......", ".GG........", "G.........."],
        ["book"] = ["TTTTTTTTT", "THMMMMMMS", "THMMTMMMS", "THMTTTMMS", "THMMTMMMS", "THMMMMMMS", "THMMMMMMS", "TTTTTTTTT", ".HHHHHHHS", "TTTTTTTTT"],
        ["scroll"] = [".TTTTTTTT.", "THHHHHHMST", ".HMMMMMMS.", ".HMMSSSMS.", ".HMMMMMMS.", ".HMMSSSMS.", ".HMMMMMMS.", "THHHHHHMST", ".TTTTTTTT."],
        ["skull"] = ["..HHHMM..", ".HHMMMMS.", "HHMMMMMMS", "HM..MM..S", "HM..MM..S", ".MMMSMMS.", "..MM.MS..", "..M.M.M.."],
        ["orb"] = ["...HHM...", ".HHMMMMS.", ".HMMMMMS.", "HMMMMMMMS", "MMMMMMMSS", ".MMMMMSS.", ".MMMSSS..", "...TTT...", "..TTTTT.."],
        ["crystal"] = ["....H....", "...HHM...", "..HHMMS..", ".HHMMMSS.", "HHMMMMSSS", ".HMMMSSS.", "..HMMSS..", "...HMS...", "....S...."],
        ["charm"] = ["...GGG...", "..G...G..", ".G.....G.", ".G.....G.", "..G...G..", "...TTT...", "..THMMT..", "..TM MST..".Replace(" ", ""), "...TST...", "....T...."],
        ["totem"] = ["..HHHMM..", "..HMMMS..", "..HM.MS..", "..HMMMS..", "..TMMMT..", "..HMMMS..", "..HMMMS..", "..HMMMS..", ".TTTTTTT.", "TTTTTTTTT"],
        ["lantern"] = ["...TTT...", "..T...T..", "..TTTTT..", "..THHMT..", "..THMMT..", "..THMMT..", "..TMMST..", "..TTTTT..", ".TTTTTTT."],
        ["potion"] = ["...GGG...", "...GGG...", "...HMH...", "...H.H...", "..H...H..", ".H.....H.", "H.HMMMMMS", "HMMMMMMMS", "HMMMMMMMS", ".HMMMMMS.", "..HHHSS.."],
        ["helm"] = [".....H.....", "...HHMMM...", "..HHMMMMS..", ".HHMMMMMMS.", ".HMMMMMMMS.", "HMMGGGGGMMS", "HMG.....GMS", "HM.......MS", "HM.......MS", ".M.......S."],
        ["hood"] = ["....HHM....", "...HHMMS...", "..HHMMMMS..", ".HHM...MMS.", ".HM.....MS.", "HM.......MS", "HM.......MS", "HM.......MS", ".HMG...GMS.", "..MMGGGMS..", "....G.G...."],
        ["cap"] = ["...HHMMM...", "..HHMMMMS..", ".HHMMMMMMS.", ".HMMMMMMMS.", "TTTTTTTTTTT", ".SSSSSSSSS."],
        ["crown"] = ["H....H....H", "HM..HMH..HM", "HMMHMMMHMMS", "HMMMMMMMMMS", "TTTTTTTTTTT"],
        ["mask"] = [".HHHHMMM.", "HHMMMMMMS", "HM..MM..S", "HM..MM..S", ".MMMMMMS.", "..MM.MS..", "..MMMMS..", "...MMS..."],
        ["plate"] = [".HM.....HM.", "HHMM...MMMS", "HMMMMMMMMMS", ".HMMMMMMMS.", ".HMMMMMMMS.", "GHMMMMMMMSG", ".HMMMMMMMS.", "GHMMMMMMMSG", ".MMMMMMMSS.", ".SSSSSSSSS."],
        ["mail"] = [".HM.....HM.", "HHMM...MMMS", "HMHMMMMMHMS", ".HMHMMMHMS.", ".HMMHMHMMS.", "GHMHMMMHMSG", ".HMMHMHMMS.", "GHMHMMMHMSG", ".MMMMMMMSS.", ".SSSSSSSSS."],
        ["coat"] = ["...HM.MS...", "..HMM.MMMS.", ".HMMM.MMMMS", "HHMMM.MMMMS", "HMHMM.MMMSS", "..HMM.MMMS.", "..HMM.MMMS.", "..GGGTGGGG.", "..HMM.MMMS.", "..MMM.MMMM.", "..SSS.SSSS."],
        ["shirt"] = ["..HM...MS..", ".HHMM.MMMS.", "HHMMMMMMMMS", "HMHMMMMMMSS", "..HMMMMMS..", "..HMMMMMS..", "..HMMMMMS..", "..GGGTGGG..", "..SSSSSSS.."],
        ["pants"] = ["GGGGTGGGG", "HMMMMMMMS", "HMMMMMMMS", "HMMMMMMMS", "HMMM.MMMS", "HMMM.MMMS", "HMMM.MMMS", "HMMM.MMMS", "SSSS.SSSS"],
        ["guards"] = ["GGGGTGGGG", ".G.....G.", "HMM...HMM", "HMMS.HMMS", "HMMS.HMMS", ".MMS.HMS.", ".HMS.HMS.", ".HMS.HMS.", ".HMS.HMS.", "TTTT.TTTT"],
        ["wrap"] = ["GGGGTGGGG", "HMMS.HMMS", "TTMS.TTMS", "HMMS.HMMS", "TTMS.TTMS", "HMMS.HMMS", "TTMS.TTMS", "SSSS.SSSS"],
        ["tassets"] = ["GGGGTGGGGGG", "HMMSHMMSHMS", "HMMSHMMSHMS", "HMMSHMMSHMS", "HMMSHMMSHMS", ".SS..SS.SS."],
        ["boot"] = [".....TTTTT", ".....HMMMS", ".....HMMMS", ".....HMMMS", ".....HMMMS", "....HMMMMS", "..HHMMMMMS", ".HMMMMMMMS", "HMMMMMMMMM", "GGGGGGGGGG"],
        ["shoe"] = ["......HMM.", ".....HMMS.", "....HTMMS.", "..HHTMMMS.", ".HMMMMMMMS", "HMMMMMMMMM", "GGGGGGGGGG"],
        ["sandals"] = [".....GMM..", "....GMMM..", "...GGMMM..", "..HMMGMMS.", ".HMMGMMMS.", "GGGGGGGGGG"],
        ["apple"] = [".....G...", "....GGG..", "..HH.MMM.", ".HHMMMMMS", ".HMMMMMMS", ".MMMMMMMS", "..MMMMMS.", "...SSSS.."],
        ["bread"] = ["..HHHHHM..", ".HHMMMMMS.", "HHMHMMHMMS", "HMMHMMHMMS", "HMMMMMMMMS", ".SSSSSSSS."],
        ["cheese"] = [".......H.", ".....HHM.", "...HHMMM.", ".HHMMSMM.", "HMMMMMMMS", "HMMSMMMMS", "SSSSSSSSS"],
        ["meat"] = [".......TT", "......TT.", "....HHM..", "..HHMMMS.", ".HMMMMMS.", "HMMMMMS..", "HMMMSS...", ".SSSS...."]
    };
}
