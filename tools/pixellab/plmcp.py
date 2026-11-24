"""PixelLab MCP 호출 도우미 (세션이 PixelLab으로 직접 그림을 뽑을 때 쓴다).

사용:
  python3 tools/pixellab/plmcp.py list                       # 도구 목록
  python3 tools/pixellab/plmcp.py call get_balance "{}"      # 남은 생성 횟수 (무료)
  python3 tools/pixellab/plmcp.py call search_help "{\"tool\": \"create_image_pixen\"}"   # 도구 설명·가격 (무료)
  python3 tools/pixellab/plmcp.py call <도구> "<JSON 인자>"

API 토큰은 여기에도 저장소에도 없다. 클라우드 환경 sleeping-sparks 의 네트워크 비밀(api.pixellab.ai)로 저장돼 있어
프록시가 요청에 넣어 준다. 토큰을 코드·문서·채팅에 적지 않는다.
체험 플랜은 한 번에 1개만 생성된다(rate limit 오류가 나면 앞 작업이 끝난 뒤 다시).
생성 전에 남은 횟수와 예상 비용을 사람에게 먼저 알린다. 기록: design/map-concept-prompts.md, design/art-direction.md
"""
import json, subprocess, sys
URL = "https://api.pixellab.ai/mcp"
def rpc(body, sid=None):
    cmd = ["curl", "-sS", "-m", "120", "-D", "-", "-X", "POST", URL, "-H", "Content-Type: application/json",
           "-H", "Accept: application/json, text/event-stream"]
    if sid: cmd += ["-H", "Mcp-Session-Id: " + sid]
    cmd += ["--data-binary", "@-"]
    out = subprocess.run(cmd, capture_output=True, text=True, input=json.dumps(body)).stdout
    sid2 = None; res = None
    for line in out.splitlines():
        if line.lower().startswith("mcp-session-id:"): sid2 = line.split(":", 1)[1].strip()
        if line.startswith("data:"):
            try: res = json.loads(line[5:])
            except ValueError: pass
        elif line.startswith("{"):
            try: res = json.loads(line)
            except ValueError: pass
    return res, sid2
def session():
    r, sid = rpc({"jsonrpc": "2.0", "id": 1, "method": "initialize", "params": {"protocolVersion": "2025-03-26", "capabilities": {}, "clientInfo": {"name": "cli", "version": "1.0"}}})
    rpc({"jsonrpc": "2.0", "method": "notifications/initialized"}, sid)
    return sid
if __name__ == "__main__":
    sid = session()
    if sys.argv[1] == "list":
        r, _ = rpc({"jsonrpc": "2.0", "id": 2, "method": "tools/list"}, sid)
        for t in r["result"]["tools"]:
            print("-", t["name"], ":", (t.get("description") or "").strip().split("\n")[0][:150])
    else:
        r, _ = rpc({"jsonrpc": "2.0", "id": 3, "method": "tools/call", "params": {"name": sys.argv[2], "arguments": json.loads(sys.argv[3]) if len(sys.argv) > 3 else {}}}, sid)
        res = r.get("result", r)
        for c in res.get("content", []):
            if c.get("type") == "text": print(c["text"])
            else: print("[", c.get("type"), len(json.dumps(c)), "bytes ]")
        if "content" not in res: print(json.dumps(res, ensure_ascii=False)[:3000])
