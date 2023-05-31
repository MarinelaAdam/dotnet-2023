using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Mapster.Common.MemoryMappedTypes;

/// <summary>
///     Action to be called when iterating over <see cref="MapFeature" /> in a given bounding box via a call to
///     <see cref="DataFile.ForeachFeature" />
/// </summary>
/// <param name="feature">The current <see cref="MapFeature" />.</param>
/// <param name="label">The label of the feature, <see cref="string.Empty" /> if not available.</param>
/// <param name="coordinates">The coordinates of the <see cref="MapFeature" />.</param>
/// <returns></returns>
public delegate bool MapFeatureDelegate(MapFeatureData featureData);

/// <summary>
///     Aggregation of all the data needed to render a map feature
/// </summary>
public readonly ref struct MapFeatureData
{
    public long Id { get; init; }
    public enum StringReplacer : byte
    {
        Admin_level = 0,
        Amenity = 1,
        Boundary = 2,
        Building = 3,
        Highway = 4,
        Landuse = 5,
        Leisure = 6,
        Name = 7,
        Natural = 8,
        Place = 9,
        Railway = 10,
        Water = 11,
        Waterway = 12,
        Water_point = 13

    }
    //public enum ValuesString : byte
    //{
    //    Unknown = 0,
    //    Hamlet = 1,
    //    Administrative = 2,
    //    City = 3,
    //    Town = 4,
    //    Farm = 5,
    //    Meadow = 6,
    //    Grass = 7,
    //    Greenfield = 8,
    //    Recreation_Ground = 9,
    //    Winter_Sports = 10,
    //    Allotments = 11,
    //    Reservoir = 12,
    //    Basin = 13,
    //    Residential = 14,
    //    Cemetery = 15,
    //    Industrial = 16,
    //    Commercial = 17,
    //    Square = 18,
    //    Construction = 19,
    //    Military = 20,
    //    Quarry = 21,
    //    Brownfield = 22,
    //    Forest = 23,
    //    Orchard = 24,
    //    Motorway = 25,
    //    Trunk = 26,
    //    Primary = 27,
    //    Secondary = 28,
    //    Tertiary = 29,
    //    Unclassified = 30,
    //    Road = 31,
    //    Fell = 32,
    //    Grassland = 33,
    //    Heath = 34,
    //    Moor = 35,
    //    Scrub = 36,
    //    Wetland = 37,
    //    Wood = 38,
    //    Tree_row = 39,
    //    Bare_rock = 40,
    //    Rock = 41,
    //    Scree = 42,
    //    Beach = 43,
    //    Sand = 44,
    //    Water = 45,
    //    Locality = 46,
    //    Two = 47,
    //    Yes = 48,
    //    Track = 49,
    //    Saddle = 50,
    //    Path = 51,
    //    Footway = 52,
    //    Service = 53,
    //    Lake = 54,
    //    Toilets = 55,
    //    Parking = 56,
    //    Pedestrian = 57,
    //    Ascensor = 58,
    //    Restaurant = 59,
    //    Primary_link = 60,
    //    College = 61,
    //    Village = 62,
    //    Neighbourhood = 63,
    //    Bus_stop = 64,
    //    Engolasters = 65,
    //    Swimming_pool = 66,
    //    Fuel = 77,
    //    Seat = 78,
    //    Motosprint = 79,
    //    Andbank = 80,
    //    Pharmacy = 81,
    //    Bar = 82,
    //    Hospital = 83,
    //    Bench = 84,
    //    Playground = 85,
    //    Bank = 86,
    //    Gril = 87,
    //    Peak = 88,
    //    Cirerer = 89,
    //    Barrera = 90,
    //    School = 91,
    //    Tous = 92,
    //    Jamaica = 93,
    //    Bbq = 94,
    //    House = 95,
    //    Tree = 96,
    //    Steps = 97,
    //    Pitch = 98,
    //    Apartments = 99,
    //    Atm = 100,

    //}

    public GeometryType Type { get; init; }
    public ReadOnlySpan<char> Label { get; init; }
    public ReadOnlySpan<Coordinate> Coordinates { get; init; }
    public Dictionary<StringReplacer, string> Properties { get; init; }
}

/// <summary>
///     Represents a file with map data organized in the following format:<br />
///     <see cref="FileHeader" /><br />
///     Array of <see cref="TileHeaderEntry" /> with <see cref="FileHeader.TileCount" /> records<br />
///     Array of tiles, each tile organized:<br />
///     <see cref="TileBlockHeader" /><br />
///     Array of <see cref="MapFeature" /> with <see cref="TileBlockHeader.FeaturesCount" /> at offset
///     <see cref="TileHeaderEntry.OffsetInBytes" /> + size of <see cref="TileBlockHeader" /> in bytes.<br />
///     Array of <see cref="Coordinate" /> with <see cref="TileBlockHeader.CoordinatesCount" /> at offset
///     <see cref="TileBlockHeader.CharactersOffsetInBytes" />.<br />
///     Array of <see cref="StringEntry" /> with <see cref="TileBlockHeader.StringCount" /> at offset
///     <see cref="TileBlockHeader.StringsOffsetInBytes" />.<br />
///     Array of <see cref="char" /> with <see cref="TileBlockHeader.CharactersCount" /> at offset
///     <see cref="TileBlockHeader.CharactersOffsetInBytes" />.<br />
/// </summary>
public unsafe class DataFile : IDisposable
{
    private readonly FileHeader* _fileHeader;
    private readonly MemoryMappedViewAccessor _mma;
    private readonly MemoryMappedFile _mmf;

    private readonly byte* _ptr;
    private readonly int CoordinateSizeInBytes = Marshal.SizeOf<Coordinate>();
    private readonly int FileHeaderSizeInBytes = Marshal.SizeOf<FileHeader>();
    private readonly int MapFeatureSizeInBytes = Marshal.SizeOf<MapFeature>();
    private readonly int StringEntrySizeInBytes = Marshal.SizeOf<StringEntry>();
    private readonly int TileBlockHeaderSizeInBytes = Marshal.SizeOf<TileBlockHeader>();
    private readonly int TileHeaderEntrySizeInBytes = Marshal.SizeOf<TileHeaderEntry>();

    private bool _disposedValue;

    public DataFile(string path)
    {
        _mmf = MemoryMappedFile.CreateFromFile(path);
        _mma = _mmf.CreateViewAccessor();
        _mma.SafeMemoryMappedViewHandle.AcquirePointer(ref _ptr);
        _fileHeader = (FileHeader*)_ptr;
    }
    public static string UpperC(string input) =>
    input switch
    {
        null => throw new ArgumentNullException(nameof(input)),
        "" => throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input)),
        _ => string.Concat(input[0].ToString().ToUpper(), input.AsSpan(1))
    };
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _mma?.SafeMemoryMappedViewHandle.ReleasePointer();
                _mma?.Dispose();
                _mmf?.Dispose();
            }

            _disposedValue = true;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private TileHeaderEntry* GetNthTileHeader(int i)
    {
        return (TileHeaderEntry*)(_ptr + i * TileHeaderEntrySizeInBytes + FileHeaderSizeInBytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private (TileBlockHeader? Tile, ulong TileOffset) GetTile(int tileId)
    {
        ulong tileOffset = 0;
        for (var i = 0; i < _fileHeader->TileCount; ++i)
        {
            var tileHeaderEntry = GetNthTileHeader(i);
            if (tileHeaderEntry->ID == tileId)
            {
                tileOffset = tileHeaderEntry->OffsetInBytes;
                return (*(TileBlockHeader*)(_ptr + tileOffset), tileOffset);
            }
        }

        return (null, 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private MapFeature* GetFeature(int i, ulong offset)
    {
        return (MapFeature*)(_ptr + offset + TileBlockHeaderSizeInBytes + i * MapFeatureSizeInBytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private ReadOnlySpan<Coordinate> GetCoordinates(ulong coordinateOffset, int ithCoordinate, int coordinateCount)
    {
        return new ReadOnlySpan<Coordinate>(_ptr + coordinateOffset + ithCoordinate * CoordinateSizeInBytes, coordinateCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private void GetString(ulong stringsOffset, ulong charsOffset, int i, out ReadOnlySpan<char> value)
    {
        var stringEntry = (StringEntry*)(_ptr + stringsOffset + i * StringEntrySizeInBytes);
        value = new ReadOnlySpan<char>(_ptr + charsOffset + stringEntry->Offset * 2, stringEntry->Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private void GetProperty(ulong stringsOffset, ulong charsOffset, int i, out ReadOnlySpan<char> key, out ReadOnlySpan<char> value)
    {
        if (i % 2 != 0)
        {
            throw new ArgumentException("Properties are key-value pairs and start at even indices in the string list (i.e. i % 2 == 0)");
        }

        GetString(stringsOffset, charsOffset, i, out key);
        GetString(stringsOffset, charsOffset, i + 1, out value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void ForeachFeature(BoundingBox b, MapFeatureDelegate? action)
    {
        if (action == null)
        {
            return;
        }

        var tiles = TiligSystem.GetTilesForBoundingBox(b.MinLat, b.MinLon, b.MaxLat, b.MaxLon);
        for (var i = 0; i < tiles.Length; ++i)
        {
            var header = GetTile(tiles[i]);
            if (header.Tile == null)
            {
                continue;
            }
            for (var j = 0; j < header.Tile.Value.FeaturesCount; ++j)
            {
                var feature = GetFeature(j, header.TileOffset);
                var coordinates = GetCoordinates(header.Tile.Value.CoordinatesOffsetInBytes, feature->CoordinateOffset, feature->CoordinateCount);
                var isFeatureInBBox = false;

                for (var k = 0; k < coordinates.Length; ++k)
                {
                    if (b.Contains(coordinates[k]))
                    {
                        isFeatureInBBox = true;
                        break;
                    }
                }

                var label = ReadOnlySpan<char>.Empty;
                if (feature->LabelOffset >= 0)
                {
                    GetString(header.Tile.Value.StringsOffsetInBytes, header.Tile.Value.CharactersOffsetInBytes, feature->LabelOffset, out label);
                }

                if (isFeatureInBBox)
                {
                    var properties = new Dictionary<MapFeatureData.StringReplacer, string>(feature->PropertyCount);
                    for (var p = 0; p < feature->PropertyCount; ++p)
                    {
                        GetProperty(header.Tile.Value.StringsOffsetInBytes, header.Tile.Value.CharactersOffsetInBytes, p * 2 + feature->PropertiesOffset, out var key, out var value);
                        var kaystr = char.ToUpper(key.ToString()[0]) + key.ToString().Substring(1);
                        var flag =  Enum.IsDefined(typeof(MapFeatureData.StringReplacer), kaystr);
                        
                        MapFeatureData.StringReplacer keyVal;

                        if (flag)
                        {

                            try
                            {
                                keyVal = (MapFeatureData.StringReplacer)Enum.Parse(typeof(MapFeatureData.StringReplacer), kaystr);
                                properties.Add(keyVal, value.ToString());

                            }
                            catch (ArgumentException)
                            {
                                Console.WriteLine("Somethig went wrong!");
                            }
                            
                        }

                        }

                    if (!action(new MapFeatureData
                        {
                            Id = feature->Id,
                            Label = label,
                            Coordinates = coordinates,
                            Type = feature->GeometryType,
                            Properties = properties
                        }))
                    {
                        break;
                    }
                }
            }
        }
    }
}
