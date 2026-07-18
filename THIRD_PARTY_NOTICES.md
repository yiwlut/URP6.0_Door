# Third-Party Notices

This file records third-party material used by `BLUES WITH YOU`. It is an attribution and distribution checklist, not a replacement for the original license texts.

## Included in the public repository

### YNL Effect wet-road and rain textures

- Copyright: Copyright (c) 2024 Yunasawa Studio
- Source: https://github.com/YNL-Collection/YNL-Effect
- License: MIT
- Local paths: `Assets/@FX/Textures/YNL/`
- Changes: Used as input textures for project-specific URP wet-street and rain shaders.
- License copy: `Assets/@FX/Textures/YNL/LICENSE-YNL-Effect.txt`

### Business Man - Low Polygon game character

- Creator: manoeldarochadeoliveira
- Source: https://sketchfab.com/3d-models/business-man-low-polygon-game-character-b6f6740f883b4749abac47af0045a9dd
- License: Creative Commons Attribution 4.0 International (CC BY 4.0)
- License URL: https://creativecommons.org/licenses/by/4.0/
- Local paths:
  - `Assets/@models/Player01/Player_01.fbx`
  - `Assets/@Materials/Player01/Textures/`
  - `Assets/@Textures/T_business_man_*.png`
- Changes: Imported for Unity Humanoid use, scaled for the player presentation, and connected to a project-specific URP Lit material. Duplicate texture copies are retained only where current Unity references require them.

Attribution statement: “Business Man - Low Polygon game character” by manoeldarochadeoliveira, licensed under CC BY 4.0. The Unity import, material setup, scale, and animation integration were modified for `BLUES WITH YOU`.

### Noto Sans KR

- Copyright: Copyright 2014-2021 Adobe, with Reserved Font Name “Source”
- Source: https://github.com/notofonts/noto-cjk
- License: SIL Open Font License 1.1
- Local path: `Assets/Font/NotoSansKR-Regular SDF.asset`
- Changes: Converted into a TextMesh Pro SDF font asset for Korean UI text.
- License copy: `Assets/Font/LICENSE-NotoSansKR-OFL.txt`

### Unity and TextMesh Pro resources

- Source: Unity 6 packages and Unity project templates
- License/status: Distributed for use with Unity under the applicable Unity package and companion terms.
- Local paths: `Assets/TextMesh Pro/`, `Assets/TutorialInfo/`, and package references in `Packages/`.

## Kept locally, excluded from the public repository

The following source assets remain available to the developer but are ignored and removed from Git tracking. Their absence from a fresh clone is intentional.

### Music and ambience

- Local paths: `Assets/@SE/*.wav` and `Assets/Resources/Audio/*.wav`
- Reason: Source and redistribution permission are not documented in the repository.
- Policy: Do not publish the raw WAV files. Record the provider, track title, purchase/download proof, and exact license before any future redistribution.

### Adobe Mixamo walking animation

- Local paths: `Assets/@Animations/Player01/*.fbx` and `Assets/@models/Player01/Walking.fbx`
- Source: Adobe Mixamo
- Usage status: Adobe documents royalty-free use of Mixamo characters and animations in video games.
- Reason for exclusion: This repository does not rely on that game-use permission as permission to redistribute the downloadable raw FBX files publicly.
- Reference: https://helpx.adobe.com/creative-cloud/faq/mixamo-faq.html

### Vending-machine source model

- Local path: `Assets/@models/VendingMachine.fbx`
- Reason: The FBX contains no usable author or license attribution, and the original source has not been documented.
- Policy: Keep excluded until the creator, source URL, and redistribution license are confirmed or replace it with a project-authored/openly licensed model.

## Project-created content

The gameplay scripts, scenes, puzzle logic, prefabs, custom ShaderLab/HLSL shaders, materials, title composition, and project-specific VFX setup were created or substantially adapted for this project. Third-party input textures and models remain governed by the licenses listed above.

## Release checklist

- Confirm the playable Web build contains only assets whose compiled distribution is permitted.
- Keep this notice and all required license files in the source repository.
- Do not re-add ignored raw WAV or FBX files without written license evidence.
- Record AI-generated or AI-assisted artwork separately in the AI usage document required for submission.
