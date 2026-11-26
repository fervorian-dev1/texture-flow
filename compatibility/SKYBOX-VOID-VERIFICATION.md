# Skybox / Void verification ? 0.4.0

Run on Windows with Unity 2022.3.22f1 and an RTX 4070, using disposable projects only.

- 65 desktop checks: all 46 pre-existing tracked file hashes unchanged; outer tabs, original three-step retention, separate painting editor, bounded mask, view/time/depth behavior, save/open settings and mask, separate request export, companion setup, traversal rejection and workspace/source independence, distinct multi-opening looks and disabled-area mapping.
- 17 Unity checks: material/shader import, stale session/expiry/path/renderer/material refusal, exact-slot assignment and other-slot preservation, restore receipt, idempotent replay and original guarded restoration; actual Direct3D11 eight-area compilation, camera movement response, visible ready-made-star parallax after texture filtering, void depth response, tunnel distinction and unchanged outside pixels.
- Live new-tab desktop apply and restore communicated with a running Unity editor and recovered the original material GUID.
- Original regression suites: 71 desktop checks, 35 Unity checks, D3D11 eight-area render test and live desktop apply/restore.

These checks do not validate VRChat upload/build, headset stereo rendering, every mesh/UV layout, every shader ecosystem, Android or sustained 60 FPS. Shader includes standard stereo instancing macros; headset output has not been observed.

Reproduce with the commands in Skybox Void Guide. Do not point fixture scripts at personal projects.
