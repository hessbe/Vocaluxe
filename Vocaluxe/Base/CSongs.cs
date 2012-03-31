using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Vocaluxe.Lib.Draw;
using Vocaluxe.Lib.Song;
using Vocaluxe.Menu;

namespace Vocaluxe.Base
{
    public struct SongPointer
    {
        public int SongID;
        public string SortString;

        public int CatIndex;
        public bool Visible;

        public SongPointer(int ID, string sortString)
        {
            SongID = ID;
            SortString = sortString;
            CatIndex = -1;
            Visible = false;
        }
    }

    public class CCategory
    {
        public string Name = String.Empty;
        public STexture CoverTextureSmall = new STexture();
        public STexture CoverTextureBig = new STexture();

        public CCategory(string name, STexture CoverSmall, STexture CoverBig)
        {
            Name = name;
            CoverTextureSmall = CoverSmall;
            CoverTextureBig = CoverBig;
        }
    }

    static class CSongs
    {
        private static List<CSong> _Songs = new List<CSong>();
        private static SongPointer[] _SongsSortList = new SongPointer[0];
        private static List<CSong> _SongsForRandom = new List<CSong>();

        private static CHelper Helper = new CHelper();
        private static bool _SongsLoaded = false;
        private static bool _CoverLoaded = false;
        private static int _CoverLoadIndex = -1;
        private static int _CatIndex = -1;
        private static List<CCategory> _Categories = new List<CCategory>();

        private static Stopwatch _CoverLoadTimer = new Stopwatch();

        private static string _SearchFilter = String.Empty;
        private static EOffOn _Tabs = CConfig.Tabs;

        public static string SearchFilter
        {
            get { return _SearchFilter; }
            set
            {
                _SearchFilter = value;
                if (_SearchFilter.Length > 0)
                {
                    _Sort(CConfig.SongSorting, EOffOn.TR_CONFIG_OFF, _SearchFilter);
                }
                else
                {
                    Sort();
                }
            }
        }

        public static EOffOn Tabs
        {
            get { return _Tabs; }
        }

        public static bool SongsLoaded
        {
            get { return _SongsLoaded; }
        }

        public static bool CoverLoaded
        {
            get 
            {
                if (_SongsLoaded && NumAllSongs == 0)
                    _CoverLoaded = true;
                return _CoverLoaded;
            }
        }

        public static int NumAllSongs
        {
            get { return _Songs.Count; }
        }

        public static int NumVisibleSongs
        {
            get
            {
                int Result = 0;
                foreach (SongPointer sp in _SongsSortList)
                {
                    if (sp.Visible)
                        Result++;
                }
                return Result;
            }
        }

        public static int NumCategories
        {
            get { return _Categories.Count; }
        }

        public static int Category
        {
            get { return _CatIndex; }
            set
            {
                if ((_Categories.Count > value) && (value >= -1))
                {
                    _CatIndex = value;

                    for (int i = 0; i < _SongsSortList.Length; i++)
                    {
                        _SongsSortList[i].Visible = (_SongsSortList[i].CatIndex == _CatIndex);
                    }
                }
            }
        }

        /// <summary>
        /// Returns the number of song in the category specified with CatIndex
        /// </summary>
        /// <param name="CatIndex">Category index</param>
        /// <returns></returns>
        public static int NumSongsInCategory(int CatIndex)
        {
            if (_Categories.Count <= CatIndex || CatIndex < 0)
                return 0;

            int num = 0;
            for (int i = 0; i < _SongsSortList.Length; i++)
            {
                if (_SongsSortList[i].CatIndex == CatIndex)
                    num++;
            }
            return num;
        }

        public static void NextCategory()
        {
            if (Category == _Categories.Count - 1)
                Category = 0;
            else
                Category++;
        }
        public static void PrevCategory()
        {
            if (Category == 0)
                Category = _Categories.Count - 1;
            else
                Category--;
        }

        public static int GetNextSongWithoutCover(ref CSong Song)
        {
            if (!SongsLoaded)
                return -1;

            if (_Songs.Count > _CoverLoadIndex + 1)
            {
                _CoverLoadIndex++;
                Song = _Songs[_CoverLoadIndex];
                return _CoverLoadIndex;
            }

            return -2;
        }
        public static int NumSongsWithCoverLoaded
        {
            get { return _CoverLoadIndex + 1; }
        }

        public static void SetCoverSmall(int SongIndex, STexture Texture)
        {
            if (!_SongsLoaded)
                return;

            if (SongIndex <= _Songs.Count)
            {
                _Songs[SongIndex].CoverTextureSmall = Texture;
                //_Songs[SongIndex].CoverSmallLoaded = true;
            }

            if (SongIndex == _Songs.Count - 1)
                _CoverLoaded = true;
        }
        public static void SetCoverBig(int SongIndex, STexture Texture)
        {
            if (!_SongsLoaded)
                return;

            if (SongIndex <= _Songs.Count)
            {
                _Songs[SongIndex].CoverTextureBig = Texture;
                _Songs[SongIndex].CoverBigLoaded = true;
            }

            if (SongIndex == _Songs.Count - 1)
                _CoverLoaded = true;
        }

        public static string GetActualCategoryName()
        {
            if ((_Categories.Count > 0) && (_CatIndex >= 0) && (_Categories.Count > _CatIndex))
                return _Categories[_CatIndex].Name;
            else
                return String.Empty;
        }

        public static CSong GetSong(int SongID)
        {
            foreach (CSong song in _Songs)
            {
                if (song.ID == SongID)
                    return song;
            }
            return null;
        }

        public static int GetVisibleSongNumber(int SongID)
        {
            int i = -1;
            foreach (CSong song in VisibleSongs)
            {
                i++;
                if (song.ID == SongID)
                    return i;
            }
            return i;
        }

        public static int GetRandomSong()
        {
            if (_SongsForRandom.Count == 0)
            {
                UpdateRandomSongList();
            }

            if (_SongsForRandom.Count == 0)
                return -1;

            CSong song = _SongsForRandom[CGame.Rand.Next(0, _SongsForRandom.Count-1)];
            _SongsForRandom.Remove(song);
            return GetVisibleSongNumber(song.ID);
        }

        public static void UpdateRandomSongList()
        {
            _SongsForRandom.Clear();
            _SongsForRandom.AddRange(VisibleSongs);
        }

        public static CSong[] AllSongs
        {
            get { return _Songs.ToArray(); }
        }

        public static CSong[] VisibleSongs
        {
            get
            {
                List<CSong> songs = new List<CSong>();
                foreach (SongPointer sp in _SongsSortList)
                {
                    if (sp.Visible)
                        songs.Add(_Songs[sp.SongID]);
                }
                return songs.ToArray();
            }
        }

        public static CCategory[] Categories
        {
            get { return _Categories.ToArray(); }
        }

        public static void Sort()
        {
            _Sort(CConfig.SongSorting, CConfig.Tabs, String.Empty);
        }

        public static void Sort(ESongSorting sorting)
        {
            _Sort(sorting, CConfig.Tabs, String.Empty);
        }

        private static List<SongPointer> _CreateSortList(string fieldName)
        {
            FieldInfo field = null;
            if(fieldName != String.Empty)
                field = Type.GetType("Vocaluxe.Lib.Song.CSong").GetField(fieldName);
            List<SongPointer> _SortList = new List<SongPointer>();
            foreach (CSong song in _Songs)
            {
                object value = field != null ? field.GetValue(song) : String.Empty;
                if (value is List<String>)
                {
                    List<String> values = (List<String>)value;
                    if (values.Count == 0)
                    {
                        _SortList.Add(new SongPointer(song.ID, String.Empty));
                    }
                    else
                    {
                        foreach (String sortString in (List<String>)value)
                        {
                            _SortList.Add(new SongPointer(song.ID, sortString));
                        }
                    }
                }
                else if (value is String)
                {
                    _SortList.Add(new SongPointer(song.ID, (String)value));
                }
                else
                {
                    throw new Exception("unkown sort field type");
                }
            }
            return _SortList;
        }
        private static int _SortByFieldArtistTitle(SongPointer s1, SongPointer s2)
        {
            int res = s1.SortString.ToUpper().CompareTo(s2.SortString.ToUpper());
            if (res == 0)
            {
                res = _Songs[s1.SongID].Artist.ToUpper().CompareTo(_Songs[s2.SongID].Artist.ToUpper());
                if (res == 0)
                {
                    return _Songs[s1.SongID].Title.ToUpper().CompareTo(_Songs[s2.SongID].Title.ToUpper());
                }
                return res;
            }
            return res;
        }
        private static int _SortByFieldTitle(SongPointer s1, SongPointer s2)
        {
            int res = s1.SortString.ToUpper().CompareTo(s2.SortString.ToUpper());
            if (res == 0)
            {
                return _Songs[s1.SongID].Title.ToUpper().CompareTo(_Songs[s2.SongID].Title.ToUpper());
            }
            return res;
        }
        private static void _CreateCategories(String NoCategoryTranslation)
        {
            string category = String.Empty;
            _Categories.Clear();
            for (int i = 0; i < _SongsSortList.Length; i++)
            {
                if (_SongsSortList[i].SortString.Length > 0)
                {
                    if (_SongsSortList[i].SortString.ToUpper() != category.ToUpper())
                    {
                        category = _SongsSortList[i].SortString;
                        _Categories.Add(new CCategory(category, new STexture(-1), new STexture(-1)));
                    }
                    _SongsSortList[i].CatIndex = _Categories.Count - 1;
                }
                else
                {
                    if (NoCategoryTranslation != category)
                    {
                        category = NoCategoryTranslation;
                        _Categories.Add(new CCategory(category, new STexture(-1), new STexture(-1)));
                    }
                    _SongsSortList[i].CatIndex = _Categories.Count - 1;
                }
            }
        }
        private static void _CreateCategoriesYear(String NoCategoryTranslation)
        {
            string category = String.Empty;
            _Categories.Clear();
            for (int i = 0; i < _SongsSortList.Length; i++)
            {
                if (_SongsSortList[i].SortString.Length > 0 && !_SongsSortList[i].SortString.Equals("0000"))
                {
                    if (_SongsSortList[i].SortString.ToUpper() != category.ToUpper())
                    {
                        category = _SongsSortList[i].SortString;
                        _Categories.Add(new CCategory(category, new STexture(-1), new STexture(-1)));
                    }
                    _SongsSortList[i].CatIndex = _Categories.Count - 1;
                }
                else
                {
                    if (NoCategoryTranslation != category)
                    {
                        category = NoCategoryTranslation;
                        _Categories.Add(new CCategory(category, new STexture(-1), new STexture(-1)));
                    }
                    _SongsSortList[i].CatIndex = _Categories.Count - 1;
                }
            }
        }
        private static void _CreateCategoriesLetter()
        {
            string category = String.Empty;
            _Categories.Clear();
            int NotLetterCat = -1;
            for (int i = 0; i < _SongsSortList.Length; i++)
            {
                Char firstLetter = Char.ToUpper(_SongsSortList[i].SortString[0]);

                if (!Char.IsLetter(firstLetter))
                {
                    firstLetter = '#';
                }
                if (firstLetter.ToString() != category)
                {
                    if (firstLetter != '#' || NotLetterCat == -1)
                    {
                        category = firstLetter.ToString();
                        _Categories.Add(new CCategory(category, new STexture(-1), new STexture(-1)));

                        _SongsSortList[i].CatIndex = _Categories.Count - 1;

                        if (firstLetter == '#')
                            NotLetterCat = _SongsSortList[i].CatIndex;
                    }
                    else
                        _SongsSortList[i].CatIndex = NotLetterCat;
                }
                else
                    _SongsSortList[i].CatIndex = _Categories.Count - 1;
            }
        }
        private static void _CreateCategoriesDecade()
        {
            string category = String.Empty;
            _Categories.Clear();
            for (int i = 0; i < _SongsSortList.Length; i++)
            {
                if (_SongsSortList[i].SortString.Length > 0 && !_SongsSortList[i].SortString.Equals("0000"))
                {
                    String decade = _SongsSortList[i].SortString.Substring(0, 3) + "0 - " + _SongsSortList[i].SortString.Substring(0, 3) + "9";
                    if (decade != category)
                    {
                        category = decade;
                        _Categories.Add(new CCategory(category, new STexture(-1), new STexture(-1)));
                    }
                    _SongsSortList[i].CatIndex = _Categories.Count - 1;
                }
                else
                {
                    if (CLanguage.Translate("TR_SCREENSONG_NOYEAR") != category)
                    {
                        category = CLanguage.Translate("TR_SCREENSONG_NOYEAR");
                        _Categories.Add(new CCategory(category, new STexture(-1), new STexture(-1)));
                    }
                    _SongsSortList[i].CatIndex = _Categories.Count - 1;
                }
            }
        }

        private static void _Sort(ESongSorting sorting, EOffOn Tabs, string SearchString)
        {
            if (_Songs.Count == 0)
                return;

            _Categories.Clear();
            string category = String.Empty;

            _Tabs = Tabs;

            List<SongPointer> _SortList = new List<SongPointer>();
            List<CSong> _SongList = new List<CSong>();

            foreach (CSong song in _Songs)
            {
                if (SearchString == String.Empty || song.Title.ToUpper().Contains(SearchString.ToUpper()) || song.Artist.ToUpper().Contains(SearchString.ToUpper()))
                {
                    _SongList.Add(song);
                }
            }

            String fieldName = String.Empty;
            switch (sorting)
            {
                case ESongSorting.TR_CONFIG_EDITION:
                    fieldName = "Edition";
                    break;
                case ESongSorting.TR_CONFIG_GENRE:
                    fieldName = "Genre";
                    break;
                case ESongSorting.TR_CONFIG_NONE:
                    fieldName = String.Empty;
                    break;
                case ESongSorting.TR_CONFIG_FOLDER:
                    fieldName = "FolderName";
                    break;
                case ESongSorting.TR_CONFIG_ARTIST_LETTER:
                case ESongSorting.TR_CONFIG_ARTIST:
                    fieldName = "Artist";
                    break;
                case ESongSorting.TR_CONFIG_TITLE_LETTER:
                    fieldName = "Title";
                    break;
                case ESongSorting.TR_CONFIG_YEAR:
                case ESongSorting.TR_CONFIG_DECADE:
                    fieldName = "Year";
                    break;
                case ESongSorting.TR_CONFIG_LANGUAGE:
                    fieldName = "Language";
                    break;
                default:
                    break;
            }
            _SortList = _CreateSortList(fieldName);
            if(sorting == ESongSorting.TR_CONFIG_NONE) 
            {
                _SortList.Sort(_SortByFieldTitle);
            }
            else 
            {
                _SortList.Sort(_SortByFieldArtistTitle);
            }
            _SongsSortList = _SortList.ToArray();
            string noCategory = string.Empty;
            switch (sorting)
            {
                case ESongSorting.TR_CONFIG_EDITION:
                    noCategory = CLanguage.Translate("TR_SCREENSONG_NOEDITION");
                    break;
                case ESongSorting.TR_CONFIG_GENRE:
                    noCategory = CLanguage.Translate("TR_SCREENSONG_NOGENRE");
                    break;
                case ESongSorting.TR_CONFIG_NONE:
                    noCategory = CLanguage.Translate("TR_SCREENSONG_ALLSONGS");
                    break;
                case ESongSorting.TR_CONFIG_ARTIST:
                case ESongSorting.TR_CONFIG_FOLDER:
                    noCategory = String.Empty;
                    break;
                case ESongSorting.TR_CONFIG_ARTIST_LETTER:
                case ESongSorting.TR_CONFIG_TITLE_LETTER:
                case ESongSorting.TR_CONFIG_DECADE:
                    break;
                case ESongSorting.TR_CONFIG_YEAR:
                    noCategory = CLanguage.Translate("TR_SCREENSONG_NOYEAR");
                    break;
                case ESongSorting.TR_CONFIG_LANGUAGE:
                    noCategory = CLanguage.Translate("TR_SCREENSONG_NOLANGUAGE");
                    break;
                default:
                    break;
            }
            switch (sorting)
            {
                case ESongSorting.TR_CONFIG_EDITION:
                case ESongSorting.TR_CONFIG_GENRE:
                case ESongSorting.TR_CONFIG_NONE:
                case ESongSorting.TR_CONFIG_ARTIST:
                case ESongSorting.TR_CONFIG_FOLDER:
                case ESongSorting.TR_CONFIG_LANGUAGE:
                    _CreateCategories(noCategory);
                    break;
                case ESongSorting.TR_CONFIG_YEAR:
                    _CreateCategoriesYear(noCategory);
                    break;
                case ESongSorting.TR_CONFIG_ARTIST_LETTER:
                case ESongSorting.TR_CONFIG_TITLE_LETTER:
                    _CreateCategoriesLetter();
                    break;
                case ESongSorting.TR_CONFIG_DECADE:
                    _CreateCategoriesDecade();
                    break;
                default:
                    break;
            }


            if (_Tabs == EOffOn.TR_CONFIG_OFF)
            {
                _Categories.Clear();
                _Categories.Add(new CCategory("", new STexture(-1), new STexture(-1)));
                for (int i = 0; i < _SongsSortList.Length; i++)
                {
                    _SongsSortList[i].CatIndex = 0;
                }
            }

            foreach (CCategory cat in _Categories)
            {
                STexture cover = CCover.Cover(cat.Name);
                cat.CoverTextureSmall = cover;
            }
        }
        public static void LoadSongs()
        {
            CLog.StartBenchmark(1, "Load Songs");
            _SongsLoaded = false;
            _Songs.Clear();

            CLog.StartBenchmark(2, "List Songs");
            List<string> files = new List<string>();
            foreach (string p in CConfig.SongFolder)
            {
                string path = p;
                files.AddRange(Helper.ListFiles(path, "*.txt", true, true));
                files.AddRange(Helper.ListFiles(path, "*.txd", true, true));
            }
            CLog.StopBenchmark(2, "List Songs");

            CLog.StartBenchmark(2, "Read TXTs");
            foreach (string file in files)
            {
                CSong Song = new CSong();
                if (Song.ReadTXTSong(file))
                {
                    Song.ID = _Songs.Count;
                    _Songs.Add(Song);
                }
            }
            CLog.StopBenchmark(2, "Read TXTs");

            CLog.StartBenchmark(2, "Sort Songs");
            Sort(CConfig.SongSorting);
            CLog.StopBenchmark(2, "Sort Songs");
            Category = -1;
            _SongsLoaded = true;

            if (CConfig.Renderer != ERenderer.TR_CONFIG_SOFTWARE && CConfig.CoverLoading == ECoverLoading.TR_CONFIG_COVERLOADING_ATSTART)
            {
                CLog.StartBenchmark(2, "Load Cover");
                for (int i = 0; i < _Songs.Count; i++)
                {
                    CSong song = _Songs[i];

                    song.ReadNotes();
                    STexture texture = song.CoverTextureSmall;
                    song.CoverTextureBig = texture;
                    _CoverLoadIndex++;
                }

                _CoverLoaded = true;
                CDataBase.CommitCovers();
                CLog.StopBenchmark(2, "Load Cover");
            }
            CLog.StopBenchmark(1, "Load Songs ");
        }

        public static void LoadCover(long WaitTime, int NumLoads)
        {
            if (CConfig.Renderer != ERenderer.TR_CONFIG_SOFTWARE)
                return; //should be removed as soon as the other renderer are ready for queque

            if (!SongsLoaded)
                return;

            if (CoverLoaded)
                return;

            if (!_CoverLoadTimer.IsRunning)
            {
                _CoverLoadTimer.Reset();
                _CoverLoadTimer.Start();
            }

            STexture texture = new STexture(-1);
            if (_CoverLoadTimer.ElapsedMilliseconds >= WaitTime)
            {
                for (int i = 0; i < NumLoads; i++)
                {
                    CSong song = new CSong();
                    int n = GetNextSongWithoutCover(ref song);

                    if (n < 0)
                        return;

                    song.ReadNotes();
                    texture = song.CoverTextureSmall;

                    SetCoverSmall(n, texture);
                    SetCoverBig(n, texture);

                    if (CoverLoaded)
                        CDataBase.CommitCovers();

                    _CoverLoadTimer.Reset();
                    _CoverLoadTimer.Start();
                }
            }
        }
    }
}
