"""Lossless, fixed-layout extraction from exemplo.png; no repainting or inpainting.
Run with Python + Pillow + numpy. Only generated output paths below are written.
"""
from pathlib import Path
import hashlib, json, math
import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2] / 'Art/UI/MainMenu'
source = Image.open(ROOT / 'exemplo.png').convert('RGBA')
W, H = source.size
assert (W,H) == (1448,1086), 'Reference size changed; review crop coordinates before regenerating.'
parts = [
 ('Logo/castleveil_logo.png', (40,40,655,408), None),
 ('Buttons/button_start.png', (65,434,587,533), None),
 ('Buttons/button_continue.png', (89,538,563,624), None),
 ('Buttons/button_options.png', (89,631,563,716), None),
 ('Buttons/button_exit.png', (120,722,532,810), None),
 ('Character/menu_character.png', (865,600,1265,978), [(991,610),(1059,600),(1103,619),(1118,653),(1156,675),(1191,716),(1204,756),(1255,720),(1265,741),(1228,831),(1205,952),(1140,978),(962,973),(865,960),(865,909),(905,880),(930,821),(929,754),(957,725),(1006,717),(1008,687),(989,654)]),
 ('Fire/campfire_00.png', (575,632,896,978), [(774,632),(832,642),(869,700),(878,830),(896,888),(867,952),(801,978),(604,972),(575,925),(633,888),(654,803),(673,706),(733,718)])
]
background = np.array(source)
manifest = {'width':W,'height':H,'sourceSha256':hashlib.sha256((ROOT/'exemplo.png').read_bytes()).hexdigest(),'parts':[],'fireFrameCount':12,'fireFramesPerSecond':10}
reconstructed = None
images=[]
for filename, box, polygon in parts:
    x,y,r,b=box
    mask=Image.new('L',(W,H))
    draw=ImageDraw.Draw(mask)
    if polygon: draw.polygon(polygon,fill=255)
    else: draw.rectangle((x,y,r-1,b-1),fill=255)
    clipped=Image.new('L',(W,H)); clipped.paste(mask.crop(box),(x,y)); mask=clipped
    layer=source.copy(); layer.putalpha(mask)
    crop=layer.crop(box)
    output=ROOT/filename; output.parent.mkdir(parents=True,exist_ok=True); crop.save(output)
    background[np.array(mask)>0,3]=0
    images.append((crop,(x,y)))
    manifest['parts'].append({'path':filename,'x':x,'y':y,'width':r-x,'height':b-y})
(ROOT/'Background').mkdir(exist_ok=True)
Image.fromarray(background).save(ROOT/'Background/menu_background.png')
reconstructed=Image.fromarray(background)
for crop, pos in images: reconstructed.alpha_composite(crop,pos)
assert np.array_equal(np.array(reconstructed),np.array(source)), 'Layers do not exactly reconstruct the reference.'
fire,position=images[-1]
base=np.array(fire); rgb=base[:,:,:3].astype(float)
y,x=np.indices(base.shape[:2]); warm=(rgb[:,:,0]>130)&(rgb[:,:,1]>60)&(rgb[:,:,2]<rgb[:,:,0]*.72)&(y<275)&(base[:,:,3]>0)
previews=[]
for index in range(12):
    phase=2*math.pi*index/12
    factor=1 + .055*math.sin(phase)*(0.6+0.4*np.sin(y*.065+x*.035))
    frame=base.copy()
    changed=np.clip(np.rint(rgb*factor[:,:,None]),0,255).astype('uint8')
    frame[:,:,:3][warm]=changed[warm]
    output=Image.fromarray(frame); output.save(ROOT/f'Fire/campfire_{index:02}.png')
    preview=reconstructed.copy(); preview.paste(output,position,output.getchannel('A'))
    previews.append(preview.convert('RGB'))
(ROOT/'Animation').mkdir(exist_ok=True)
reconstructed.save(ROOT/'Animation/menu_reconstruction.png')
# Shared palette avoids unrelated color flicker in the review GIF.
palette=previews[0].quantize(colors=256)
frames=[im.quantize(palette=palette,dither=Image.Dither.NONE) for im in previews]
frames[0].save(ROOT/'Animation/campfire_preview.gif',save_all=True,append_images=frames[1:],duration=100,loop=0,disposal=2)
(ROOT/'layout.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
print(f'Created {len(parts)+13} PNGs (including background/reconstruction), 12-frame loop; reconstruction pixel difference = 0.')
