# Artwork edit — version 1.2.1

September 15, 2026. Edited with the built-in image generation tool, one paired crop atlas per pose. The editor removed the gray credit lettering from 32 sprites; the classic pair was already clear. Reconstructed patches were fitted back to the original images using their original boundary colors. Original alpha, dimensions, silhouettes and all pixels outside those areas are unchanged.

All final sprites are saved in `FableAstra/Assets/`. The file names and conversation cues are unchanged. Artist attribution remains in `CREDITS.md`, and the supplied reference file is preserved unchanged.

## Exact shared prompt

Use case: precise-object-edit. The input image is the EDIT TARGET: a 1024 by 512 atlas containing two square cropped areas of existing anime character artwork, left and right. Remove ONLY the gray '@thatlev' watermark lettering from BOTH halves. Completely erase every trace of these gray letters and reconstruct the natural cloth shading, folds, or underlying surface behind them. Keep ALL other pixels and features as unchanged and spatially aligned as possible: same clothing, fold boundaries, linework, colors, edges, hands, held objects and existing large black character-name printing when visible. Preserve the exact atlas composition: two 512x512 panels, left/right positions fixed, same zoom and crop, no shifts, no added margin. Do not redraw, restyle, sharpen, alter contrast, zoom, move objects, introduce seams, or add text. This is a localized retouch, not a new illustration. Return only the edited atlas, same dimensions and aspect ratio.

## Pose-specific additions

- **original:** On the RIGHT the watermark crosses a bare forearm and a purple galaxy skirt: reconstruct both the natural skin and the galaxy pattern, keeping the arm contour exactly where it is.
- **happy:** Remove the malformed/faint duplicate gray credit lettering on the RIGHT as well; retain the large black ASTRA 6 printing.

## Edited sprite pairs

- **adventure:** [Fable](FableAstra/Assets/adventure-fable.png) · [Astra](FableAstra/Assets/adventure-astra.png)
- **bake:** [Fable](FableAstra/Assets/bake-fable.png) · [Astra](FableAstra/Assets/bake-astra.png)
- **blanket:** [Fable](FableAstra/Assets/blanket-fable.png) · [Astra](FableAstra/Assets/blanket-astra.png)
- **book:** [Fable](FableAstra/Assets/book-fable.png) · [Astra](FableAstra/Assets/book-astra.png)
- **cat:** [Fable](FableAstra/Assets/cat-fable.png) · [Astra](FableAstra/Assets/cat-astra.png)
- **drink:** [Fable](FableAstra/Assets/drink-fable.png) · [Astra](FableAstra/Assets/drink-astra.png)
- **experiment:** [Fable](FableAstra/Assets/experiment-fable.png) · [Astra](FableAstra/Assets/experiment-astra.png)
- **game:** [Fable](FableAstra/Assets/game-fable.png) · [Astra](FableAstra/Assets/game-astra.png)
- **garden:** [Fable](FableAstra/Assets/garden-fable.png) · [Astra](FableAstra/Assets/garden-astra.png)
- **happy:** [Fable](FableAstra/Assets/happy-fable.png) · [Astra](FableAstra/Assets/happy-astra.png)
- **notes:** [Fable](FableAstra/Assets/notes-fable.png) · [Astra](FableAstra/Assets/notes-astra.png)
- **original:** [Fable](FableAstra/Assets/original-fable.png) · [Astra](FableAstra/Assets/original-astra.png)
- **snack:** [Fable](FableAstra/Assets/snack-fable.png) · [Astra](FableAstra/Assets/snack-astra.png)
- **soup:** [Fable](FableAstra/Assets/soup-fable.png) · [Astra](FableAstra/Assets/soup-astra.png)
- **space:** [Fable](FableAstra/Assets/space-fable.png) · [Astra](FableAstra/Assets/space-astra.png)
- **talk:** [Fable](FableAstra/Assets/talk-fable.png) · [Astra](FableAstra/Assets/talk-astra.png)
