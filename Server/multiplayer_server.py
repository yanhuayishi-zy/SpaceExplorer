#!/usr/bin/env python3
"""
太空探险 WebSocket 联机中继服务器
部署: pip install websockets
运行: python3 multiplayer_server.py --host 0.0.0.0 --port 8765
Nginx 可用 location /game/ { proxy_pass http://127.0.0.1:8765/; ... WebSocket upgrade }
"""
import asyncio
import json
import argparse
import time
import uuid
from collections import defaultdict

try:
    import websockets
    from websockets.server import serve
except ImportError:
    print("请先安装: pip install websockets")
    raise

# room_id -> { players: {pid: ws}, host_pid, state }
ROOMS = {}
# ws -> (room_id, pid)
WS_INFO = {}


def now():
    return time.time()


async def send_json(ws, obj):
    if ws.closed:
        return
    try:
        await ws.send(json.dumps(obj, ensure_ascii=False))
    except Exception:
        pass


async def broadcast_room(room_id, obj, exclude_pid=None):
    room = ROOMS.get(room_id)
    if not room:
        return
    data = json.dumps(obj, ensure_ascii=False)
    dead = []
    for pid, ws in room["players"].items():
        if exclude_pid and pid == exclude_pid:
            continue
        try:
            if not ws.closed:
                await ws.send(data)
        except Exception:
            dead.append(pid)
    for pid in dead:
        await cleanup_player(room_id, pid)


async def cleanup_player(room_id, pid):
    room = ROOMS.get(room_id)
    if not room:
        return
    room["players"].pop(pid, None)
    await broadcast_room(room_id, {"t": "leave", "pid": pid})
    if not room["players"]:
        ROOMS.pop(room_id, None)
        return
    if room.get("host_pid") == pid:
        room["host_pid"] = next(iter(room["players"]))
        await broadcast_room(room_id, {"t": "host", "pid": room["host_pid"]})


async def handler(ws):
    pid = str(uuid.uuid4())[:8]
    room_id = None
    try:
        await send_json(ws, {"t": "welcome", "pid": pid, "ts": now()})
        async for raw in ws:
            try:
                msg = json.loads(raw)
            except Exception:
                continue
            t = msg.get("t")

            if t == "create":
                room_id = msg.get("room") or str(uuid.uuid4())[:4].upper()
                if room_id in ROOMS and len(ROOMS[room_id]["players"]) >= 2:
                    await send_json(ws, {"t": "error", "msg": "房间已满"})
                    continue
                if room_id not in ROOMS:
                    ROOMS[room_id] = {
                        "players": {},
                        "host_pid": pid,
                        "created": now(),
                        "seed": int(msg.get("seed", 0)) or int(now() * 1000) % 100000,
                    }
                ROOMS[room_id]["players"][pid] = ws
                WS_INFO[ws] = (room_id, pid)
                await send_json(ws, {
                    "t": "joined",
                    "room": room_id,
                    "pid": pid,
                    "host": ROOMS[room_id]["host_pid"],
                    "seed": ROOMS[room_id]["seed"],
                    "players": list(ROOMS[room_id]["players"].keys()),
                })
                await broadcast_room(room_id, {"t": "join", "pid": pid}, exclude_pid=pid)

            elif t == "join":
                room_id = str(msg.get("room", "")).strip().upper()
                room = ROOMS.get(room_id)
                if not room:
                    await send_json(ws, {"t": "error", "msg": "房间不存在"})
                    continue
                if len(room["players"]) >= 2:
                    await send_json(ws, {"t": "error", "msg": "房间已满"})
                    continue
                room["players"][pid] = ws
                WS_INFO[ws] = (room_id, pid)
                await send_json(ws, {
                    "t": "joined",
                    "room": room_id,
                    "pid": pid,
                    "host": room["host_pid"],
                    "seed": room["seed"],
                    "players": list(room["players"].keys()),
                })
                await broadcast_room(room_id, {"t": "join", "pid": pid}, exclude_pid=pid)

            elif t == "state" and room_id:
                # 转发位置/输入等，附上 pid
                msg["pid"] = pid
                msg["ts"] = now()
                await broadcast_room(room_id, msg, exclude_pid=pid)

            elif t == "event" and room_id:
                msg["pid"] = pid
                await broadcast_room(room_id, msg, exclude_pid=pid)

            elif t == "ping":
                await send_json(ws, {"t": "pong", "ts": now()})

            elif t == "leave":
                break

    except websockets.exceptions.ConnectionClosed:
        pass
    finally:
        info = WS_INFO.pop(ws, None)
        if info:
            await cleanup_player(info[0], info[1])
        elif room_id:
            await cleanup_player(room_id, pid)


async def reaper():
    while True:
        await asyncio.sleep(30)
        dead = [rid for rid, r in ROOMS.items() if not r["players"]]
        for rid in dead:
            ROOMS.pop(rid, None)


async def main(host, port):
    print(f"太空探险联机中继启动 ws://{host}:{port}")
    async with serve(handler, host, port, ping_interval=20, ping_timeout=20, max_size=2**20):
        await reaper()


if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("--host", default="0.0.0.0")
    ap.add_argument("--port", type=int, default=8765)
    args = ap.parse_args()
    try:
        asyncio.run(main(args.host, args.port))
    except KeyboardInterrupt:
        print("已停止")
