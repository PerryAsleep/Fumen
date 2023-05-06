# PadData

`PadData` represents how dance pads are laid out for a [ChartType](ChartType.md) and how the arrows can be combined to form various moves. See [StepTypes](StepTypes.md) for details on these moves.

The `PadData` files provided in the library are automatically generated from simplified input through [PadDataGenerator](../../PadDataGenerator/docs/Readme.md). `PadData` file names match their `ChartType`, e.g. `dance-single.json`.

## Coordinates

`PadData` arrow coordinates are represented by X and Y integer values where X goes left to right and Y goes front to back.

### Example

```
dance-double: [(0,1), (1,2), (1,0), (2,1), (0,1), (1,2), (1,0), (2,1)]
         _______                 _______        
        |       |               |       |       
        | (1,0) |               | (3,0) |       
 _______|_______|_______ _______|_______|_______
|       |       |       |       |       |       |
| (0,1) |       | (2,1) | (2,1) |       | (4,1) |
|_______|_______|_______|_______|_______|_______|
        |       |               |       |       
        | (1,2) |               | (3,2) |       
        |_______|               |_______|       
```

## File Format

`PadData` files are json with support for comments and trailing commas.

### Pad Data

- **YTravelDistanceCompensation**: Number (double) type. A value to help take into account that movements between equally spaced and equally sized panels in Y take less foot movement to travel between than movements for panels separated by the same distance in X due to the length of a foot being significantly greater than the width of a foot. This value is used by various [PerformedChart](PerformedChart.md) [step tightening parameters](PerformedChart.md/#step-tightening). A value of `1.0` for this parameter corresponds to the length of one panel.

- **StartingPositions**: Array type. Each value in this array is a tier of positions with lower index tiers being preferred to higher index tiers. The value at each tier is an array of equally preferred starting positions on the pad. A position is an array of two number (integer) values with the first index corresponding to the left foot and the second index corresponding to the right foot. The value at each index is in the index of the arrow for the foot to start on. When creating a `PerformedChart`, searches begin using the starting position at the first tier and if no path could be found the application will try using the starting positions at the next tier, and so on. When multiple positions exist at the same tier they are considered equally preferred and will be chosen in a random order. It is required that there be at least one tier and it is required that the first tier have exactly one position in it.

- **ArrowData**: Array type. Each value is an object describing each panel on the pad. See [Arrow Data](#arrow-data) below for more details.

### Arrow Data

#### Coordinates

- **X**: Number (integer) type. X Position of the panel relative to other panels.
- **Y**: Number (integer) type. Y Position of the panel relative to other panels.

#### Pairing Arrays

For the array types below the index is the foot under consideration. It is expected this is of length 2, with the first index being the left foot and the second index being the right foot. For each foot, the value is another array where the index in that array is the arrow index in the pads. The value at that index is a boolean which defines whether that arrow forms a valid pairing with this arrow and the original foot.

```json
// Example OtherFootPairings for dance-single arrow 0: the left arrow.
"OtherFootPairings": [
    [ false, true,  true,  true ], // Left foot on left arrow can support the right foot on any other arrow without twisting.
    [ false, false, false, false]  // Right foot on left arrow doesn't support the left foot on any other arrow without twisting.
],
```

- **BracketablePairingsHeel**: When the toe is on this ArrowData's arrow, whether this corresponding arrow forms a bracket using the heel.
- **BracketablePairingsToe**: When the heel is on this ArrowData's arrow, whether this corresponding arrow forms a bracket using the toe.
- **OtherFootPairings**: When a foot is on this ArrowData's arrow, whether this corresponding arrow is a valid pairing for the other foot without crossovers, inverts, or stretch.
- **OtherFootPairingsStretch**: When a foot is on this ArrowData's arrow, whether this corresponding arrow is a valid stretch pairing for the other foot without crossovers or inverts.
- **OtherFootPairingsCrossoverFront**: When a foot is on this ArrowData's arrow, whether this corresponding arrow is a valid crossover in front without stretch.
- **OtherFootPairingsCrossoverFrontStretch**: When a foot is on this ArrowData's arrow, whether this corresponding arrow is a valid crossover in front with stretch.
- **OtherFootPairingsCrossoverBehind**: When a foot is on this ArrowData's arrow, whether this corresponding arrow is a valid crossover in back without stretch.
- **OtherFootPairingsCrossoverBehindStretch**: When a foot is on this ArrowData's arrow, whether this corresponding arrow is a valid crossover in back with stretch.
- **OtherFootPairingsInverted**: When a foot is on this ArrowData's arrow, whether this corresponding arrow is a valid invert without stretch.
- **OtherFootPairingsInvertedStretch**: When a foot is on this ArrowData's arrow, whether this corresponding arrow is a valid invert with stretch.

## Example: dance-single.json

```json
{
  "StartingPositions": [
    [
      [
        0,
        3
      ]
    ],
    [
      [
        0,
        1
      ],
      [
        1,
        3
      ],
      [
        0,
        2
      ],
      [
        2,
        3
      ]
    ],
    [
      [
        1,
        2
      ],
      [
        2,
        1
      ]
    ]
  ],
  "ArrowData": [
    {
      "X": 0,
      "Y": 1,
      "BracketablePairingsOtherHeel": [
        [
          false,
          true,
          false,
          false
        ],
        [
          false,
          true,
          false,
          false
        ]
      ],
      "BracketablePairingsOtherToe": [
        [
          false,
          false,
          true,
          false
        ],
        [
          false,
          false,
          true,
          false
        ]
      ],
      "OtherFootPairings": [
        [
          false,
          true,
          true,
          true
        ],
        [
          false,
          false,
          false,
          false
        ]
      ],
      "OtherFootPairingsStretch": [
        [
          false,
          false,
          false,
          false
        ],
        [
          false,
          false,
          false,
          false
        ]
      ],
      "OtherFootPairingsCrossoverFront": [
        [
          false,
          false,
          false,
          false
        ],
        [
          false,
          false,
          true,
          false
        ]
      ],
      "OtherFootPairingsCrossoverFrontStretch": [
        [
          false,
          false,
          false,
          false
        ],
        [
          false,
          false,
          false,
          false
        ]
      ],
      "OtherFootPairingsCrossoverBehind": [
        [
          false,
          false,
          false,
          false
        ],
        [
          false,
          true,
          false,
          false
        ]
      ],
      "OtherFootPairingsCrossoverBehindStretch": [
        [
          false,
          false,
          false,
          false
        ],
        [
          false,
          false,
          false,
          false
        ]
      ],
      "OtherFootPairingsInverted": [
        [
          false,
          false,
          false,
          false
        ],
        [
          false,
          false,
          false,
          true
        ]
      ],
      "OtherFootPairingsInvertedStretch": [
        [
          false,
          false,
          false,
          false
        ],
        [
          false,
          false,
          false,
          false
        ]
      ]
    },
    {
      "X": 1,
      "Y": 2,
      "BracketablePairingsOtherHeel": [
        [
          false,
          false,
          false,
          false
        ],
        [
          false,
          false,
          false,
          false
        ]
      ],
      "BracketablePairingsOtherToe": [
        [
          true,
          false,
          false,
          true
        ],
        [
          true,
          false,
          false,
          true
        ]
      ],
      "OtherFootPairings": [
        [
          false,
          false,
          true,
          true
        ],
        [
          true,
          false,
          true,
          false
        ]
      ],
      "OtherFootPairingsStretch": [
        [
          false,
          false,
          false,
          false
        ],
        [
          false,
          false,
          false,
          false
        ]
      ],
      "OtherFootPairingsCrossoverFront": [
        [
          true,
          false,
          false,
          false
        ],
        [
          false,
          false,
          false,
          true
        ]
      ],
      "OtherFootPairingsCrossoverFrontStretch": [
        [
          false,
          false,
          false,
          false
        ],
        [
          false,
          false,
          false,
          false
        ]
      ],
      "OtherFootPairingsCrossoverBehind": [
        [
          false,
          false,
          false,
          false
        ],
        [
          false,
          false,
          false,
          false
        ]
      ],
      "OtherFootPairingsCrossoverBehindStretch": [
        [
          false,
          false,
          false,
          false
        ],
        [
          false,
          false,
          false,
          false
        ]
      ],
      "OtherFootPairingsInverted": [
        [
          false,
          false,
          false,
          false
        ],
        [
          false,
          false,
          false,
          false
        ]
      ],
      "OtherFootPairingsInvertedStretch": [
        [
          false,
          false,
          false,
          false
        ],
        [
          false,
          false,
          false,
          false
        ]
      ]
    },
    {
      "X": 1,
      "Y": 0,
      "BracketablePairingsOtherHeel": [
        [
          true,
          false,
          false,
          true
        ],
        [
          true,
          false,
          false,
          true
        ]
      ],
      "BracketablePairingsOtherToe": [
        [
          false,
          false,
          false,
          false
        ],
        [
          false,
          false,
          false,
          false
        ]
      ],
      "OtherFootPairings": [
        [
          false,
          true,
          false,
          true
        ],
        [
          true,
          true,
          false,
          false
        ]
      ],
      "OtherFootPairingsStretch": [
        [
          false,
          false,
          false,
          false
        ],
        [
          false,
          false,
          false,
          false
        ]
      ],
      "OtherFootPairingsCrossoverFront": [
        [
          false,
          false,
          false,
          false
        ],
        [
          false,
          false,
          false,
          false
        ]
      ],
      "OtherFootPairingsCrossoverFrontStretch": [
        [
          false,
          false,
          false,
          false
        ],
        [
          false,
          false,
          false,
          false
        ]
      ],
      "OtherFootPairingsCrossoverBehind": [
        [
          true,
          false,
          false,
          false
        ],
        [
          false,
          false,
          false,
          true
        ]
      ],
      "OtherFootPairingsCrossoverBehindStretch": [
        [
          false,
          false,
          false,
          false
        ],
        [
          false,
          false,
          false,
          false
        ]
      ],
      "OtherFootPairingsInverted": [
        [
          false,
          false,
          false,
          false
        ],
        [
          false,
          false,
          false,
          false
        ]
      ],
      "OtherFootPairingsInvertedStretch": [
        [
          false,
          false,
          false,
          false
        ],
        [
          false,
          false,
          false,
          false
        ]
      ]
    },
    {
      "X": 2,
      "Y": 1,
      "BracketablePairingsOtherHeel": [
        [
          false,
          true,
          false,
          false
        ],
        [
          false,
          true,
          false,
          false
        ]
      ],
      "BracketablePairingsOtherToe": [
        [
          false,
          false,
          true,
          false
        ],
        [
          false,
          false,
          true,
          false
        ]
      ],
      "OtherFootPairings": [
        [
          false,
          false,
          false,
          false
        ],
        [
          true,
          true,
          true,
          false
        ]
      ],
      "OtherFootPairingsStretch": [
        [
          false,
          false,
          false,
          false
        ],
        [
          false,
          false,
          false,
          false
        ]
      ],
      "OtherFootPairingsCrossoverFront": [
        [
          false,
          false,
          true,
          false
        ],
        [
          false,
          false,
          false,
          false
        ]
      ],
      "OtherFootPairingsCrossoverFrontStretch": [
        [
          false,
          false,
          false,
          false
        ],
        [
          false,
          false,
          false,
          false
        ]
      ],
      "OtherFootPairingsCrossoverBehind": [
        [
          false,
          true,
          false,
          false
        ],
        [
          false,
          false,
          false,
          false
        ]
      ],
      "OtherFootPairingsCrossoverBehindStretch": [
        [
          false,
          false,
          false,
          false
        ],
        [
          false,
          false,
          false,
          false
        ]
      ],
      "OtherFootPairingsInverted": [
        [
          true,
          false,
          false,
          false
        ],
        [
          false,
          false,
          false,
          false
        ]
      ],
      "OtherFootPairingsInvertedStretch": [
        [
          false,
          false,
          false,
          false
        ],
        [
          false,
          false,
          false,
          false
        ]
      ]
    }
  ],
  "YTravelDistanceCompensation": 0.5
}
```