# FO4 NPC Manager

## Licence

This project is licensed under the **GNU General Public License version 3**.
The full text is in `LICENSE`. Credits are in `LICENSE_CREDITS.txt`, and the
per-component copyright lines, licence texts and source-code offer are in
`THIRD-PARTY-NOTICES.md` and the `licenses/` folder.

## Requires the following libraries/packages

 - ManoloV02: FO4 Base Library - Licensed under the GPL-3.0 License (https://github.com/MANOLOV02/FO4_Base_Library)
 - ManoloV02: BSA/BA2 Library - Licensed under the GPL-3.0 License (https://github.com/MANOLOV02/BSA_BA2_Library_DLL)
 - ManoloV02: DirectXTexWrapper - Licensed under the GPL-3.0 License (https://github.com/MANOLOV02/DirectXTexWrapper)
 - Ousnius: NiflySharp - Licensed under the GPL-3.0 License (https://github.com/MANOLOV02/NiflySharpFork)
     MODIFICADO. Fork de https://github.com/ousnius/NiflySharp con cambios propios; el fuente correspondiente es el del fork
 - Lukas Cone: HavokLib - Licensed under the GPL-3.0 License (https://github.com/PredatorCZ/HavokLib)
     hkaLosslessCompressedAnimation decoder ported into FO4_Base_Library/HkxLosslessAnimationGraphParser.vb
 - Microsoft: DirectXTex - Licensed under the MIT License (https://github.com/microsoft/DirectXTex)
     wrapped by DirectXTexWrapper
 - Ousnius: Material Editor - Licensed under the MIT License (https://github.com/ousnius/Material-Editor)
 - Stefanos Apostolopoulos: OpenTK (Core, Graphics, Mathematics, Windowing) - Licensed under the MIT License (https://github.com/opentk/opentk)
     Copyright (c) 2006-2018 Stefanos Apostolopoulos, for the Open Toolkit library
 - Team OpenTK: OpenTK.GLControl - Licensed under the MIT License (https://github.com/opentk/opentk)
     Copyright (c) 2025 Team OpenTK
 - Marcus Geelnard, Camilla Löwy: GLFW (glfw3.dll) - Licensed under the zlib/libpng License (https://www.glfw.org)
     redistributed through OpenTK.redist.glfw
 - SharpZipLib Contributors: SharpZipLib 1.4.2 - Licensed under the MIT License (https://github.com/icsharpcode/SharpZipLib)
 - Milosz Krajewski: K4os.Compression.LZ4 (+ .Streams) - Licensed under the MIT License (https://github.com/MiloszKrajewski/K4os.Compression.LZ4)
     Copyright (c) 2017 Milosz Krajewski
 - Milosz Krajewski: K4os.Hash.xxHash - Licensed under the MIT License (https://github.com/MiloszKrajewski/K4os.Hash.xxHash)
     Copyright (c) 2017 Milosz Krajewski
 - Lorenzo Delana: miniball - Licensed under the Apache-2.0 License (https://github.com/SearchAThing-forks/miniball)
     fork of https://github.com/hbf/miniball by Martin Kutz (FU Berlin), Kaspar Fischer (ETH Zurich) and Bernd Gaertner (ETH Zurich); reached through NiflySharp
 - Microsoft: System.IO.Pipelines - Licensed under the MIT License (https://github.com/dotnet/runtime)
 - Microsoft: Ijwhost (Ijwhost.dll) - Licensed under the MIT License (https://github.com/dotnet/runtime)
     C++/CLI host shim required by DirectXTexWrapper
 - ElminsterAU and the xEdit contributors: xEdit - Licensed under the MPL-2.0 License (https://github.com/TES5Edit/TES5Edit)
     the plugin format declarations are mechanically translated into the schema tables; see THIRD-PARTY-NOTICES.md

## Build

Build with MSBuild, configuration `Publish`:

```
msbuild FO4_NPC_Manager/NPC_Manager_FO4.vbproj -t:Restore,Build -p:Configuration=Publish -p:Platform=x64
```

The pinned Windows build and synthetic checks run in
[GitHub Actions](.github/workflows/windows-build.yml). The GUI uses `Publish|x64`;
the FaceTint CLI uses `Release|x64` for its native dependencies.

## Experimental adult NPC transformation

The [headless production coordinator](docs/engineering/PRODUCTION_INTEGRATION.md) reuses the editor's
LooksMenu parser, override applier, save shadow, ESP writer and FaceGen generator. It stages small
explicit adult-human batches, retains failed attempts and supports hash-verified resume. The Windows
CLI is distributed only in experimental CI artifacts; it never installs a patch.

The [Windows integration gate](Tools/PluginRoundTripGate/README.md) uses generated binary NPC_
plugins to exercise actual production APIs. Fixture success and compilation are distinct from
real FaceGen or in-game evidence. See [implementation and limits](docs/engineering/PRODUCTION_INTEGRATION.md).
The initial synthetic motor is retained as a regression baseline.

To run its assertions with .NET SDK 8.0.408, use a new output filename:

```text
cd Tools/AdultTransformationGate
dotnet run --configuration Release -- experimental-report.json
```

Outputs remain experimental until real plugin, FaceGen, dialogue, quest and collection
compatibility checks pass. No game installation is required for these synthetic checks.

## Global Fallout 4 GOTY / DLC / WoD campaign planning

The [global architecture and local CLI](docs/engineering/GLOBAL_CONVERTER.md) now discover winning
NPC_ records from an explicit active load order, retain override chains and classify conservative
category/adult policies. Deterministic small pages feed the existing production pipeline without
manual NPC selection in the GUI. Plans and prepared pages are not converted characters.

Generic adult humans have priority; unique/companion/special cases require regression review.
Robots/creatures have separate pending redesign routes; child/unknown adulthood stays blocked.
The global executor remains pending hard output quota, localized-master export, asset/body and
narrative verification. Resources stay in the isolated local profile; no collection upload/copy.

## Autonomous engineering and local collection inspection

Start with [AGENTS.md](AGENTS.md) and the [persistent execution plan](docs/engineering/EXECUTION_PLAN.md).
The [requirement matrix](docs/engineering/requirements.json), [evidence log](docs/engineering/EVIDENCE.md)
and [blocker register](docs/engineering/BLOCKERS.md) distinguish synthetic, binary and in-game validation.

The [offline Windows WoD inspector](Tools/WoDInspector/README.md) inventories local installations
and BA2 member names without uploading the collection or extracting assets. The
[recurring automation proposal](docs/engineering/AUTOMATIONS.md) remains disabled pending explicit approval.

Localized/Traits integration and safe preparation: [implementation and boundaries](docs/engineering/LOCALIZED_AND_TRAITS.md).
