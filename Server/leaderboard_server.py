#!/usr/bin/env python3
"""
SpaceExplorer leaderboard + WebGL host.
API: /health /submit /top
Game static: ./webgl/ (index.html etc.)
"""
from flask import Flask, request, jsonify, send_from_directory, abort, make_response
from pathlib import Path
import json
import os
import threading

BASE = Path(os.environ.get("LB_DATA_DIR", Path(__file__).parent)).resolve()
WEBGL = BASE / "webgl"
DATA = BASE / "leaderboard.json"
LOCK = threading.Lock()
PORT = int(os.environ.get("LB_PORT", "18080"))
HOST = os.environ.get("LB_HOST", "127.0.0.1")

app = Flask(__name__)

API_PATHS = {"/health", "/submit", "/top", "/avatar"}
AVATAR_DIR = BASE / "avatars"


def load():
    if DATA.exists():
        try:
            return json.loads(DATA.read_text(encoding="utf-8"))
        except Exception:
            return []
    return []


def save(rows):
    DATA.parent.mkdir(parents=True, exist_ok=True)
    DATA.write_text(json.dumps(rows, ensure_ascii=False, indent=2), encoding="utf-8")


@app.after_request
def cors(resp):
    # 同域托管时通常不需要；保留以便跨域调试
    resp.headers["Access-Control-Allow-Origin"] = "*"
    resp.headers["Access-Control-Allow-Headers"] = "Content-Type"
    resp.headers["Access-Control-Allow-Methods"] = "GET,POST,OPTIONS"
    return resp


@app.route("/health")
def health():
    return jsonify({
        "ok": True,
        "service": "spaceexplorer-leaderboard",
        "game": (WEBGL / "index.html").exists(),
    })


@app.route("/submit", methods=["POST", "OPTIONS"])
def submit():
    if request.method == "OPTIONS":
        return ("", 204)
    body = request.get_json(silent=True) or {}
    name = str(body.get("name", "匿名"))[:32]
    try:
        score = int(body.get("score", 0))
    except Exception:
        score = 0
    if score < 0:
        score = 0
    hide = bool(body.get("hide", False))
    display = body.get("display") or name
    display = str(display)[:32]
    avatar = str(body.get("avatar", ""))[:32]
    title = str(body.get("title", ""))[:32]

    with LOCK:
        rows = load()
        found = False
        for r in rows:
            if r.get("name") == name:
                if score > int(r.get("score", 0)):
                    r["score"] = score
                r["display"] = display
                r["hide"] = hide
                r["avatar"] = avatar
                r["title"] = title
                found = True
                break
        if not found:
            rows.append({
                "name": name, "score": score, "display": display,
                "hide": hide, "avatar": avatar, "title": title
            })
        rows.sort(key=lambda x: int(x.get("score", 0)), reverse=True)
        rows = rows[:50]
        save(rows)
    return jsonify({"ok": True, "name": name, "score": score})


@app.route("/top")
def top():
    with LOCK:
        rows = load()
    return jsonify(rows[:20])


@app.route("/avatar", methods=["POST", "OPTIONS"])
def upload_avatar():
    if request.method == "OPTIONS":
        return ("", 204)
    body = request.get_json(silent=True) or {}
    name = str(body.get("name", ""))[:32]
    image = str(body.get("image", ""))
    if not name or not image:
        return jsonify({"ok": False, "error": "missing"}), 400
    try:
        import base64
        raw = base64.b64decode(image)
        if len(raw) < 32 or len(raw) > 2_000_000:
            return jsonify({"ok": False, "error": "bad image"}), 400
        AVATAR_DIR.mkdir(parents=True, exist_ok=True)
        # 文件名安全化
        safe = "".join(ch for ch in name if ch.isalnum() or ch in "_-") or "anon"
        (AVATAR_DIR / (safe + ".png")).write_bytes(raw)
        return jsonify({"ok": True, "name": name})
    except Exception as e:
        return jsonify({"ok": False, "error": str(e)}), 400


@app.route("/avatar/<path:name>")
def get_avatar(name):
    import base64
    safe = "".join(ch for ch in name if ch.isalnum() or ch in "_-") or ""
    if not safe:
        abort(404)
    path = AVATAR_DIR / (safe + ".png")
    if not path.is_file():
        abort(404)
    resp = make_response(path.read_bytes())
    resp.headers["Content-Type"] = "image/png"
    resp.headers["Cache-Control"] = "public, max-age=3600"
    return resp


@app.route("/")
def index():
    index_file = WEBGL / "index.html"
    if not index_file.exists():
        return (
            "<!doctype html><meta charset='utf-8'><title>SpaceExplorer</title>"
            "<h1>排行榜服务在线</h1>"
            "<p>游戏包尚未上传。健康检查：<a href='/health'>/health</a> · "
            "排行：<a href='/top'>/top</a></p>",
            200,
        )
    data = index_file.read_bytes()
    resp = make_response(data)
    resp.headers["Content-Type"] = "text/html; charset=utf-8"
    # 首页始终拉最新，资源靠 ?v= 缓存破坏
    resp.headers["Cache-Control"] = "no-cache, no-store, must-revalidate"
    return resp


@app.route("/<path:path>")
def static_files(path):
    # API 优先
    if "/" + path.split("/")[0] in API_PATHS or path.startswith("avatar/"):
        abort(404)
    if not WEBGL.exists():
        abort(404)
    full = (WEBGL / path).resolve()
    if not str(full).startswith(str(WEBGL)):
        abort(404)
    if full.is_file():
        return send_from_directory(WEBGL, path)
    # Unity 生成路径兜底
    if (WEBGL / path / "index.html").is_file():
        return send_from_directory(WEBGL, path + "/index.html")
    abort(404)


if __name__ == "__main__":
    print(f"Leaderboard+Game on http://{HOST}:{PORT}")
    print(f"Data: {DATA}")
    print(f"WebGL: {WEBGL} exists={(WEBGL / 'index.html').exists()}")
    app.run(host=HOST, port=PORT, threaded=True)
