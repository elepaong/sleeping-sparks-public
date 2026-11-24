# data/stages/ 의 스테이지를 모두 굽는다 (index.json 순서).
#   python3 unity/ArtSource/tools/bake_all.py [스테이지 id ...]
# 결과: unity/Assets/Resources/SparkForest/stages/<id>/ (게임이 읽는 파일), unity/ArtSource/preview/<id>/ (확인용 그림)
import json, os, shutil, subprocess, sys
HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, '..', '..', '..'))
STAGES = os.path.join(REPO, 'data', 'stages')
RES = os.path.join(REPO, 'unity', 'Assets', 'Resources', 'SparkForest')
PREV = os.path.join(REPO, 'unity', 'ArtSource', 'preview')
index = json.load(open(os.path.join(STAGES, 'index.json'), encoding='utf-8'))
ids = sys.argv[1:] or index['stages']
for sid in ids:
    print('==', sid)
    subprocess.run([sys.executable, 'gen_map.py', os.path.join(STAGES, sid + '.json'),
                    os.path.join(RES, 'stages', sid), os.path.join(PREV, sid)], cwd=HERE, check=True)
shutil.copyfile(os.path.join(STAGES, 'index.json'), os.path.join(RES, 'stages.json'))
print('index ->', os.path.join(RES, 'stages.json'))
