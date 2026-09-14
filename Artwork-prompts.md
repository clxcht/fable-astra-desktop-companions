# Artwork prompts

Generated with the built-in image generation tool. The attached user image was the reference/edit target in both calls. Generated RGB images had baked checkerboards; the user explicitly approved code-based background removal. Final transparent sprite pairs are under `FableAstra/Assets/`.

## Classic illustration

Use case: background-extraction. Asset type: transparent PNG sprite for a Windows desktop companion. Input image 1 is the edit target. Extract BOTH existing adult anime characters from the attached image exactly as they are: Fable chan on the left (long copper hair, white Fable 5.1 shirt, brown shorts) and Astra chan on the right (dark purple hair, cat ears and galaxy tail, white ASTRA 6 shirt, galaxy skirt). Preserve their original faces, outfits, lettering, pose, composition, colors, artist marks and all visible body pixels as faithfully as possible. Remove ONLY the surrounding white/black background and the detached speech bubble line fragments. Genuine transparent alpha background; no checkerboard, no ground, no shadow, no new accessories, no labels. Keep characters separate and maintain original relative left-right positions. Retain the original knee-cropped framing without inventing lower legs. Tight balanced canvas around both characters. Do not redesign them.

## Happy illustration

Edit the original input into a new matching anime illustration for a desktop companion app. These same two adult characters, Fable chan copper hair on the left and Astra chan dark violet catgirl on the right, are laughing together. Fable has an open cheerful smile and waves, Astra smiles with closed eyes and her hand near her cheek. Preserve the original drawing style, recognizable faces, white named T-shirts, brown shorts for Fable, violet galaxy skirt and tail for Astra. Knee-up composition. Both characters fully inside frame with a clear gap between them. Give this PNG a TRUE transparent alpha channel everywhere outside the two character silhouettes, including the gap. DO NOT draw a checkerboard transparency grid. DO NOT draw any background at all, no scenery, no ground, no decorative stars. If transparency cannot be provided, use perfectly uniform solid white instead; never checkerboard. Preserve artist credit @thatlev unobtrusively on the shirts. No speech bubbles, captions or other elements.

## Default original sprites

The default is not a generated redraw. `original-fable.png` and `original-astra.png` are cutouts from the user's original image. The original PNG is also bundled unchanged. Cleanup removes both outside backgrounds and manually identified enclosed background pockets, reconstructs edge colors to remove white/checkerboard contamination, and applies partial transparency to soften the original screenshot's pale contour. Interior colors are preserved.
## Tea break sprites (version 1.1)

Use case: illustration-story. Asset type: two anime character sprites for a Windows desktop companion, in the exact character design and illustration style of the supplied reference. Same adult characters Fable chan on the LEFT and Astra chan on the RIGHT. Fable: long copper orange hair, brown eyes, original white Fable 5.1 t-shirt and brown shorts. Astra: long black-violet hair with galaxy highlights, purple cat ears and tail, purple eyes, original white ASTRA 6 t-shirt and galaxy skirt with black thigh-highs. New POSE: both having a cozy tea break. Fable holds a small warm terracotta mug with both hands near her lips, eyes softly smiling. Astra holds a small lavender mug near her mouth, calmly sipping with half-closed eyes. Preserve body proportions, facial identities, shirt lettering and artist credit @thatlev. Frame both from the top of their hair to just above the knees, at matching heights. Each figure entirely separate, large clear gap between the two; hands, mugs, hair and tail entirely inside the canvas. Both standing; no furniture, no background objects. Crisp clean dark outer contour. IMPORTANT use a perfectly uniform solid bright cyan background color #00FFEA everywhere outside the character silhouettes, including enclosed holes in hair and mug handles; this is a chroma-key asset. NO checkerboard, no white background, no gradients, no shadows or glows outside the silhouettes, no steam, no speech bubbles. Cyan must not appear within their clothes, skin or mugs. The source image is a character/style reference; create the new poses.

## Talking sprites (version 1.1)

Use case: illustration-story. Create a new pair of anime character conversation sprites matching the supplied reference exactly in character design and drawing style. These same two adult women: Fable chan LEFT with copper hair, white Fable 5.1 T-shirt and brown shorts; Astra chan RIGHT with dark violet galaxy hair, purple cat ears, white ASTRA 6 T-shirt, galaxy skirt, black thigh-highs and galaxy cat tail. NEW TALKING POSE: Fable is speaking cheerfully with a small naturally open mouth and one raised index finger as if she has an idea, other hand resting on her hip. Astra is speaking with a small open smile and one open palm in a gentle explaining gesture, other hand relaxed. Each faces slightly inward, but neither overlaps the other. Preserve facial identities, proportions, original shirt lettering and artist credit @thatlev. Knee-up framing, tops of hair fully visible, both same height, generous clear gap between figures. All hands and tail completely inside frame. Crisp clean dark silhouettes. Background MUST be one uniform solid bright cyan #00FFEA, also filling all enclosed gaps in hair and between limbs. No checkerboard, no white background, no glows, no shadows outside figures, no decorative icons, no speech bubbles, no furniture. Do not use cyan inside their costumes or skin. Intended for chroma-key extraction into transparent PNGs. Reference is design/style only; make these new speaking poses.

These pairs used the built-in image generator with the original as the character reference. Code then removed the cyan matte, including enclosed gaps, reconstructed the edge transparency, and removed cyan spill. Final assets: `FableAstra/Assets/drink-fable.png`, `drink-astra.png`, `talk-fable.png`, and `talk-astra.png`.

## Conversation action sprites (version 1.2)

Built-in image generation, one call per paired pose. The previous drinking illustration (`work/drink-generated.png`) was the character and style reference for every call. The shared prompt below was followed verbatim by the corresponding action prompt. Code-based background removal was explicitly authorized by the user. The cyan was removed from outer and enclosed gaps, edge alpha reconstructed, and cyan spill removed. All 24 resulting PNGs have real transparency.

### Shared prompt

Use case: illustration-story. Asset type: paired anime sprites for a Windows desktop companion. Reference image is character/style reference only. Draw these same two adult women with the reference's crisp linework, soft cel shading, faces and proportions: Fable chan LEFT, long copper hair, brown eyes, white 'Fable 5.1' t-shirt, brown shorts; Astra chan RIGHT, long black-violet galaxy hair, purple eyes, purple cat ears and tail, white 'ASTRA 6' t-shirt, galaxy skirt and black thigh-highs. Preserve their identity, outfit lettering, original small sleeve logos and subtle @thatlev credit. Stand knee-up at equal height, hair tips fully inside top, thighs cut by the bottom edge, all hands and props fully in frame, tail completely inside frame. Both face slightly inward. Two clearly separated figures with a generous empty central gap. Held props must touch a hand or torso, nothing detached. No furniture, no scenery, no speech bubbles, no floor or outside shadows. Clean dark contours. Production chroma-key background: perfectly uniform solid bright cyan #00FFEA everywhere outside their silhouettes, including every enclosed hole; no cyan in characters or props. No checkerboard, white background, halos, steam or floating decorations.

### soup

NEW POSE: both tasting soup with curious, amused expressions. Each supports a small ceramic bowl of warm orange soup in one hand at chest height and lifts a spoon in the other. Fable's warm cream bowl has a slice of crusty bread tucked against its rim. Astra's purple bowl also includes a bread slice. Keep bowls below shirt titles where possible. Each has a small conversational smile, spoon near but not covering lips.

Final assets: `FableAstra/Assets/soup-fable.png` and `FableAstra/Assets/soup-astra.png`.

### experiment

NEW POSE: a playful controlled soup experiment. Both wear clear protective laboratory goggles with dark rims over their eyes, their normal outfits still fully visible. Fable holds a small cream bowl of orange soup with a bread slice against the bowl and carefully measures it with a kitchen probe thermometer. Astra holds a small purple bowl of soup and examines a filled measuring spoon critically. Curious focused expressions and small open speaking mouths. Friendly kitchen science, no chemicals, no flasks, no lab coats.

Final assets: `FableAstra/Assets/experiment-fable.png` and `FableAstra/Assets/experiment-astra.png`.

### bake

NEW POSE: making a cosmic bakery dessert. Fable happily holds a small plate with a cupcake and a star-shaped cookie in one hand and pipes icing onto the cupcake with a small piping bag in the other. Astra holds a small plate with an iced cupcake and star-shaped cookies, lifting one cookie to inspect the frosting. Amused lively expressions, small speaking smiles. No apron, preserve original clothes. Everything held securely near torso.

Final assets: `FableAstra/Assets/bake-fable.png` and `FableAstra/Assets/bake-astra.png`.

### snack

NEW POSE: enjoying a cozy snack break. Fable holds a small plate with golden biscuits and a few cheese cubes in one hand and holds a biscuit close to her smiling mouth in the other. Astra holds a small matching purple plate of biscuits and cheese cubes at chest level and points playfully at one biscuit. Warm happy expressions, both eyes open and conversational. No mugs in this pose. Original outfits unchanged.

Final assets: `FableAstra/Assets/snack-fable.png` and `FableAstra/Assets/snack-astra.png`.

### book

NEW POSE: reading a story together. Each holds an open small book with both hands just below the chest, one fingertip keeping the page. Fable glances up with an excited smile as if she reached a plot twist. Astra peers over the edge of her purple hardback with an amused knowing smile. Pages have simple faint unreadable lines, no large captions.

Final assets: `FableAstra/Assets/book-fable.png` and `FableAstra/Assets/book-astra.png`.

### notes

NEW POSE: writing down creative ideas and solving a riddle. Each holds a small clipboard with a sheet of paper against her torso and a yellow pencil in the other hand. Fable eagerly writes a first sentence, her pencil tip touching the page. Astra holds the pencil near her cheek while thinking, one raised eyebrow, slight conversational smile. A few pale gold sticky notes overlap the lower paper. Use small unreadable marks, no captions.

Final assets: `FableAstra/Assets/notes-fable.png` and `FableAstra/Assets/notes-astra.png`.

### space

NEW POSE: stargazing and discussing a tiny planet. Fable cradles a small grey cratered model moon in one hand and holds a rolled parchment star chart in the other, delighted. Astra holds a compact brass handheld telescope diagonally upward near her shoulder, not on a tripod, with her other hand holding a small dark-purple star chart folded against her waist. Attentive conversational smiles, faces unobstructed. No floating stars, no celestial backgrounds.

Final assets: `FableAstra/Assets/space-fable.png` and `FableAstra/Assets/space-astra.png`.

### garden

NEW POSE: growing flowers for a moon greenhouse. Fable holds a small terracotta pot of daisies against her torso with one hand and a small brass watering can in the other, spout resting near the pot, no visible water stream. Astra holds a small purple pot with violet flowers against her torso and writes on a plant label using a pencil. Green leaves are natural dark leaf green, not bright cyan or teal. Sweet satisfied speaking expressions, original outfits unchanged.

Final assets: `FableAstra/Assets/garden-fable.png` and `FableAstra/Assets/garden-astra.png`.

### game

NEW POSE: playing a cozy video game and discussing a save point. Each holds a small handheld game console with both hands at chest height, thumbs on the controls. Fable has an ivory and copper handheld and beams toward her friend. Astra has a purple handheld, her mouth a mischievous speaking smile. No wires, no floating interface or button icons. Original outfits unchanged, proportions identical to reference.

Final assets: `FableAstra/Assets/game-fable.png` and `FableAstra/Assets/game-astra.png`.

### blanket

NEW POSE: a quiet bedtime blanket break. Both are standing with soft small blankets draped over shoulders and gathered in hands at chest height, relaxed sleepy gentle smiles, eyes visible. Fable has a warm cream fluffy blanket, Astra a muted deep-purple blanket with small gold star embroidery. The blankets end at their WAISTS, leaving their original shorts, galaxy skirt and thighs clearly visible. Original hair and cat tail fully visible. Do not turn the blankets into dresses, no beds or pillows.

Final assets: `FableAstra/Assets/blanket-fable.png` and `FableAstra/Assets/blanket-astra.png`.

### cat

NEW POSE: cat-on-keyboard conversation. Fable holds a small compact ivory computer keyboard horizontally against her waist with one forearm; a tiny orange tabby cat lies asleep on the keyboard. Fable gently pets its head with her free hand, amused soft speaking smile. Astra cradles a tiny sleeping black kitten against her torso with one arm and offers one small golden cat treat in the other hand, affectionate wry smile. Cats are about the size of a mug, not large; props stay inside each figure's shoulder width. No extra limbs, no furniture, no floating sleep symbols.

Final assets: `FableAstra/Assets/cat-fable.png` and `FableAstra/Assets/cat-astra.png`.

### adventure

NEW POSE: playful debate about one enormous duck versus a hundred tiny dragons. Fable gently holds one tiny friendly red baby dragon in cupped hands against her chest, delighted small open smile. The dragon has two tiny wings, small legs, warm red-orange scales, not cyan or teal, and is only the size of a mug. Astra holds a little golden yellow duck plush toy in one hand against her chest and a small stack of cream paper name tags in the other, dry amused expression. No weapons, no combat, no background creatures; everything held.

Final assets: `FableAstra/Assets/adventure-fable.png` and `FableAstra/Assets/adventure-astra.png`.
