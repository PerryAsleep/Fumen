# StepTypes

The library understands the following `StepTypes`. Jumps are represented as one `StepType` per foot at the same time. For a given pad layout, [PadData](PadData.md) is used for determining which arrows form which kinds of steps with other arrows. For example, which steps are considered to be stretch steps comes from the `PadData`.

## Simple Steps

- **SameArrow**: The foot steps on an arrow it is currently resting on.
- **NewArrow**: The foot steps on a new unoccupied arrow without crossing over, inverting, or stretching.
- **CrossoverFront**: The foot steps on a new arrow, crossing over in front of the other foot without stretching.
- **CrossoverBehind**: The foot steps on a new arrow, crossing over in back of the other foot without stretching.
- **InvertFront**: The foot steps on a new arrow, crossing over in front of the other foot and ending in an orientation facing away directly away from the machine.
- **InvertBehind**: The foot steps on a new arrow, crossing over in back of the other foot and ending in an orientation facing away directly away from the machine.
- **FootSwap**: The foot steps on a new arrow that the other foot was resting on.
- **Swing**: The foot begins crossed over or inverted and steps on a new arrow resulting in being crossed over in the other direction or inverted in the other direction, swinging around the other leg in the process.

## Simple Stretch Steps

Strech steps are distinct from other steps in that they result in the player's legs being spread apart far enough that they should be considered a distinct type.
- **NewArrowStretch**: The foot steps on a new unoccupied arrow while stretching without crossing over or inverting.
- **CrossoverFrontStretch**: The foot steps on a new arrow while stretching, crossing over in front of the other foot.
- **CrossoverBehindStretch**: The foot steps on a new arrow while stretching, crossing over in back of the other foot.
- **InvertFrontStretch**: The foot steps on a new arrow while stretching, crossing over in front of the other foot and ending in an orientation facing away directly away from the machine.
- **InvertBehindStretch**: The foot steps on a new arrow while stretching, crossing over in back of the other foot and ending in an orientation facing away directly away from the machine.

## Brackets

Brackets are steps that involve one foot stepping on multiple arrows simulteneously. The library supports brackets of up to two arrows. When bracketing the heel portion of the foot steps on one arrow while the toe steps on the other arrow.

### Simple Brackets

- **BracketHeelNewToeNew**: Both the heel and toe portions of the foot move to unoccupied new arrows.
- **BracketHeelNewToeSame**: The heel portion steps on an unoccupied new arrow and the toe portion steps on an arrow that this foot was already resting on.
- **BracketHeelSameToeNew**: The heel portion steps on an arrow that this foot was already resting on and the toe portion steps on an unoccupied new arrow.
- **BracketHeelSameToeSame**: Both the heel and toe portions of the foot step on arrows that this foot was already resting on.

### Foot Swap Brackets

- **BracketHeelSameToeSwap**: The heel portion steps on an arrow that this foot was already resting on and the toe portion steps on an arrow that the other foot was resting on.
- **BracketHeelNewToeSwap**: The heel portion steps on an unoccupied new arrow and the toe portion steps on an arrow that the other foot was resting on.
- **BracketHeelSwapToeSame**: The heel portion steps on an arrow that the other foot was resting on and the toe portion steps on an arrow that this foot was already resting on.
- **BracketHeelSwapToeNew**: The heel portion steps on an arrow that the other foot was resting on and the toe portion steps on an unoccupied new arrow.
- **BracketHeelSwapToeSwap**: The heel and toe portions both step on arrows that the other foot was resting on.

### Swing Brackets

- **BracketSwing**: Both the heel and toe portions of the foot move to unoccupied new arrows, performing a swing move in the process.

### Crossover Brackets

Crossover brackets are bracket moves where at least one arrow in the bracket pair forms a crossover with at least one arrow occupied by the other foot, and neither arrow is inverted with any arrow occupied by the other foot.
- **BracketCrossoverFrontHeelNewToeNew**: Both the heel and toe portions of the foot move to unoccupied new arrows, ending crossed over in front of the other foot.
- **BracketCrossoverFrontHeelNewToeSame**: The heel portion steps on an unoccupied new arrow and the toe portion steps on an arrow that this foot was already resting on, ending crossed over in front of the other foot.
- **BracketCrossoverFrontHeelSameToeNew**: The heel portion steps on an arrow that this foot was already resting on and the toe portion steps on an unoccupied new arrow, ending crossed over in front of the other foot.
- **BracketCrossoverBehindHeelNewToeNew**: Both the heel and toe portions of the foot move to unoccupied new arrows, ending crossed over in back of the other foot.
- **BracketCrossoverBehindHeelNewToeSame**: The heel portion steps on an unoccupied new arrow and the toe portion steps on an arrow that this foot was already resting on, ending crossed over in back of the other foot.
- **BracketCrossoverBehindHeelSameToeNew**: The heel portion steps on an arrow that this foot was already resting on and the toe portion steps on an unoccupied new arrow, ending crossed over in back of the other foot.

### Invert Brackets

Invert brackets are bracket moves where at least one arrow in the bracket pair forms an invert with at least one arrow occupied by the other foot.
- **BracketInvertFrontHeelNewToeNew**: Both the heel and toe portions of the foot move to unoccupied new arrows, ending inverted in front of the other foot.
- **BracketInvertFrontHeelNewToeSame**: The heel portion steps on an unoccupied new arrow and the toe portion steps on an arrow that this foot was already resting on, ending inverted in front of the other foot.
- **BracketInvertFrontHeelSameToeNew**: The heel portion steps on an arrow that this foot was already resting on and the toe portion steps on an unoccupied new arrow, ending inverted in front of the other foot.
- **BracketInvertBehindHeelNewToeNew**: Both the heel and toe portions of the foot move to unoccupied new arrows, ending inverted in back of the other foot.
- **BracketInvertBehindHeelNewToeSame**: The heel portion steps on an unoccupied new arrow and the toe portion steps on an arrow that this foot was already resting on, ending inverted in back of the other foot.
- **BracketInvertBehindHeelSameToeNew**: The heel portion steps on an arrow that this foot was already resting on and the toe portion steps on an unoccupied new arrow, ending inverted in back of the other foot.

### Stretch Brackets

For a bracket move to be considered stretch at least two of all foot portion combinations must be stretch.
- **BracketStretchHeelNewToeNew**: Both the heel and toe portions of the foot move to unoccupied new arrows while stretching.
- **BracketStretchHeelNewToeSame**: The heel portion steps on an unoccupied new arrow and the toe portion steps on an arrow that this foot was already resting on while stretching
- **BracketStretchHeelSameToeNew**: The heel portion steps on an arrow that this foot was already resting on and the toe portion steps on an unoccupied new arrow while stretching.

## Single Arrow Brackets

Single arrow brackets are steps that involve one foot stepping on one arrow with only one portion of the foot due to holding another arrow with the other portion of the foot.

### Simple Single Arrow Brackets

- **BracketOneArrowHeelSame**: While the toe portion of the foot is holding the heel portion steps on an arrow that this foot was already resting on.
- **BracketOneArrowHeelNew**: While the toe portion of the foot is holding the heel portion steps on an unoccupied new arrow.
- **BracketOneArrowToeSame**: While the heel portion of the foot is holding the toe portion steps on an arrow that this foot was already resting on.
- **BracketOneArrowToeNew**: While the heel portion of the foot is holding the toe portion steps on an unoccupied new arrow.

### Foot Swap Single Arrow Brackets

- **BracketOneArrowHeelSwap**: While the toe portion of the foot is holding the heel portion steps on an arrow the other foot was resting on.
- **BracketOneArrowToeSwap**: While the heel portion of the foot is holding the toe portion steps on an arrow the other foot was resting on.

### Crossover Single Arrow Brackets

Like crossover brackets, crossover single arrow brackets are moves where at least one arrow in the bracket pair forms a crossover with at least one arrow occupied by the other foot, and neither arrow is inverted with any arrow occupied by the other foot.
- **BracketCrossoverFrontOneArrowHeelNew**: While the toe portion of the foot is holding the heel portion steps on an unoccupied new arrow, ending crossed over in front of the other foot.
- **BracketCrossoverFrontOneArrowToeNew**: While the heel portion of the foot is holding the toe portion steps on an unoccupied new arrow, ending crossed over in front of the other foot.
- **BracketCrossoverBehindOneArrowHeelNew**: While the toe portion of the foot is holding the heel portion steps on an unoccupied new arrow, ending crossed over in back of the other foot.
- **BracketCrossoverBehindOneArrowToeNew**: While the heel portion of the foot is holding the toe portion steps on an unoccupied new arrow, ending crossed over in back of the other foot.

### Invert Single Arrow Brackets

Like invert brackets, invert single arrow brackets are bracket moves where at least one arrow in the bracket pair forms an invert with at least one arrow occupied by the other foot.
- **BracketInvertFrontOneArrowHeelNew**:  While the toe portion of the foot is holding the heel portion steps on an unoccupied new arrow, ending inverted in front of the other foot.
- **BracketInvertFrontOneArrowToeNew**: While the heel portion of the foot is holding the toe portion steps on an unoccupied new arrow, ending inverted in front of the other foot.
- **BracketInvertBehindOneArrowHeelNew**: While the toe portion of the foot is holding the heel portion steps on an unoccupied new arrow, ending inverted in back of the other foot.
- **BracketInvertBehindOneArrowToeNew**: While the heel portion of the foot is holding the toe portion steps on an unoccupied new arrow, ending inverted in back of the other foot.

### Stretch Single Arrow Brackets

For a bracket move to be considered stretch at least two of all foot portion combinations must be stretch.
- **BracketStretchOneArrowHeelNew**: While the toe portion of the foot is holding the heel portion steps on an unoccupied new arrow while stretching.
- **BracketStretchOneArrowToeNew**: While the heel portion of the foot is holding the toe portion steps on an unoccupied new arrow while stretching.
