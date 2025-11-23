import os
import pickle
import pandas as pd
import xml.etree.ElementTree as ET
import json
from datetime import datetime
from math import radians, sin, cos, sqrt, atan2
from sklearn.linear_model import LinearRegression

# Default file paths
DATA_CSV = "Running.csv"
MODEL_FILE = "Running.pkl"


def _haversine_m(lat1, lon1, lat2, lon2):
    R = 6371000.0
    dlat = radians(lat2 - lat1)
    dlon = radians(lon2 - lon1)
    a = sin(dlat / 2) ** 2 + cos(radians(lat1)) * cos(radians(lat2)) * sin(dlon / 2) ** 2
    c = 2 * atan2(sqrt(a), sqrt(1 - a))
    return R * c


def _compute_features_from_points(points, times):
    """
    points: list of (lat, lon, ele_or_None)
    times : list of ISO8601 time strings (may be empty)
    """
    if not points:
        return {
            'distance_km': 0.0,
            'elevation_gain_m': 0.0,
            'slope': 0.0,
            'total_time_s': None
        }

    total_dist = 0.0
    total_gain = 0.0
    prev_lat = prev_lon = prev_ele = None

    for (lat, lon, ele) in points:
        if prev_lat is not None and prev_lon is not None:
            total_dist += _haversine_m(prev_lat, prev_lon, lat, lon)

        if prev_ele is not None and ele is not None:
            delta = ele - prev_ele
            if delta > 0:
                total_gain += delta

        prev_lat, prev_lon, prev_ele = lat, lon, ele

    distance_km = total_dist / 1000.0
    slope = (total_gain / total_dist) if total_dist > 0 else 0.0

    total_time_s = None
    if len(times) >= 2:
        t0 = datetime.fromisoformat(times[0].replace('Z', '+00:00'))
        t1 = datetime.fromisoformat(times[-1].replace('Z', '+00:00'))
        total_time_s = (t1 - t0).total_seconds()

    return {
        'distance_km': distance_km,
        'elevation_gain_m': total_gain,
        'slope': slope,
        'total_time_s': total_time_s
    }


# ---------- TCX parsing ----------

def parse_tcx(file_path: str) -> dict:
    tree = ET.parse(file_path)
    root = tree.getroot()
    ns = {'tcx': 'http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2'}

    points = []
    times = []

    for tp in root.findall('.//tcx:Trackpoint', ns):
        # Time
        time_elem = tp.find('tcx:Time', ns)
        if time_elem is not None and time_elem.text:
            times.append(time_elem.text)

        # Altitude
        ele_elem = tp.find('tcx:AltitudeMeters', ns)
        ele = float(ele_elem.text) if (ele_elem is not None and ele_elem.text) else None

        # Position
        lat_elem = tp.find('.//tcx:LatitudeDegrees', ns)
        lon_elem = tp.find('.//tcx:LongitudeDegrees', ns)
        if lat_elem is None or lon_elem is None or not lat_elem.text or not lon_elem.text:
            continue
        lat = float(lat_elem.text)
        lon = float(lon_elem.text)

        points.append((lat, lon, ele))

    return _compute_features_from_points(points, times)


def _parse_trackpoints_tcx(file_path: str):
    tree = ET.parse(file_path)
    root = tree.getroot()
    ns = {'tcx': 'http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2'}
    pts = []
    for tp in root.findall('.//tcx:Trackpoint', ns):
        lat_e = tp.find('.//tcx:LatitudeDegrees', ns)
        lon_e = tp.find('.//tcx:LongitudeDegrees', ns)
        ele_e = tp.find('tcx:AltitudeMeters', ns)
        if lat_e is None or lon_e is None or not lat_e.text or not lon_e.text:
            continue
        lat = float(lat_e.text)
        lon = float(lon_e.text)
        ele = float(ele_e.text) if (ele_e is not None and ele_e.text) else None
        pts.append((lat, lon, ele))
    return pts


# ---------- GPX parsing ----------

def _discover_gpx_ns(root):
    if root.tag.startswith('{'):
        uri = root.tag[1:].split('}')[0]
        return {'gpx': uri}
    return {}


def parse_gpx(file_path: str) -> dict:
    tree = ET.parse(file_path)
    root = tree.getroot()
    ns = _discover_gpx_ns(root)

    if ns:
        trkpts = root.findall('.//gpx:trkpt', ns)
    else:
        trkpts = root.findall('.//trkpt')

    points = []
    times = []

    for tp in trkpts:
        lat_attr = tp.get('lat')
        lon_attr = tp.get('lon')
        if not lat_attr or not lon_attr:
            continue
        lat = float(lat_attr)
        lon = float(lon_attr)

        if ns:
            ele_elem = tp.find('gpx:ele', ns)
            time_elem = tp.find('gpx:time', ns)
        else:
            ele_elem = tp.find('ele')
            time_elem = tp.find('time')

        ele = float(ele_elem.text) if (ele_elem is not None and ele_elem.text) else None
        if time_elem is not None and time_elem.text:
            times.append(time_elem.text)

        points.append((lat, lon, ele))

    return _compute_features_from_points(points, times)


def _parse_trackpoints_gpx(file_path: str):
    tree = ET.parse(file_path)
    root = tree.getroot()
    ns = _discover_gpx_ns(root)

    if ns:
        trkpts = root.findall('.//gpx:trkpt', ns)
    else:
        trkpts = root.findall('.//trkpt')

    pts = []
    for tp in trkpts:
        lat_attr = tp.get('lat')
        lon_attr = tp.get('lon')
        if not lat_attr or not lon_attr:
            continue
        lat = float(lat_attr)
        lon = float(lon_attr)

        if ns:
            ele_elem = tp.find('gpx:ele', ns)
        else:
            ele_elem = tp.find('ele')

        ele = float(ele_elem.text) if (ele_elem is not None and ele_elem.text) else None
        pts.append((lat, lon, ele))
    return pts


# ---------- unified parsing (TCX + GPX) ----------

def parse_activity(file_path: str) -> dict:
    ext = os.path.splitext(file_path)[1].lower()
    if ext == '.tcx':
        return parse_tcx(file_path)
    elif ext == '.gpx':
        return parse_gpx(file_path)
    else:
        raise ValueError(f"Unsupported file type: {file_path}. Use .tcx or .gpx")


def _parse_trackpoints(file_path: str):
    ext = os.path.splitext(file_path)[1].lower()
    if ext == '.tcx':
        return _parse_trackpoints_tcx(file_path)
    elif ext == '.gpx':
        return _parse_trackpoints_gpx(file_path)
    else:
        raise ValueError(f"Unsupported file type: {file_path}. Use .tcx or .gpx")


def _cumulative_distances_m(pts):
    cum = [0.0]
    acc = 0.0
    for (la1, lo1, _), (la2, lo2, _) in zip(pts, pts[1:]):
        acc += _haversine_m(la1, lo1, la2, lo2)
        cum.append(acc)
    return cum


def _lerp(a, b, t):
    return a + (b - a) * t


# ---------- data/model ----------

def ensure_data_store(csv_path: str = DATA_CSV):
    if not os.path.exists(csv_path):
        pd.DataFrame(columns=["distance_km", "elevation_gain_m", "slope", "total_time_s"]).to_csv(csv_path, index=False)


def add_activity(file_path: str, csv_path: str = DATA_CSV) -> dict:
    feats = parse_activity(file_path)
    if feats['total_time_s'] is None:
        raise ValueError("Could not extract total_time_s from file.")
    ensure_data_store(csv_path)
    df = pd.read_csv(csv_path)
    df = pd.concat([df, pd.DataFrame([feats])], ignore_index=True) if not df.empty else pd.DataFrame([feats])
    df.to_csv(csv_path, index=False)
    return feats


def train_model(csv_path: str = DATA_CSV, model_path: str = MODEL_FILE) -> LinearRegression:
    df = pd.read_csv(csv_path)
    if df.shape[0] < 2:
        raise ValueError("Need at least two data points to train a regression model.")
    X = df[["distance_km", "elevation_gain_m", "slope"]]
    y = df["total_time_s"]
    model = LinearRegression()
    model.fit(X, y)
    with open(model_path, "wb") as f:
        pickle.dump(model, f)
    return model


def load_model(model_path: str = MODEL_FILE) -> LinearRegression:
    if not os.path.exists(model_path):
        raise FileNotFoundError(f"Model file not found: {model_path}")
    with open(model_path, "rb") as f:
        return pickle.load(f)


def predict_total_seconds(file_path: str, model_path: str = MODEL_FILE, csv_path: str = DATA_CSV) -> float:
    feats = parse_activity(file_path)
    try:
        model = load_model(model_path)
        X = [[feats['distance_km'], feats['elevation_gain_m'], feats['slope']]]
        return float(model.predict(X)[0])
    except (FileNotFoundError, ValueError):
        df = pd.read_csv(csv_path)
        if df.shape[0] == 0:
            raise RuntimeError("No data available for fallback prediction.")
        base = df.iloc[0]
        pace_s_per_km = base['total_time_s'] / base['distance_km'] if base['distance_km'] > 0 else 0.0
        return pace_s_per_km * feats['distance_km']


def coordinate_at_seconds(path: str, seconds: float, model_path: str = MODEL_FILE, csv_path: str = DATA_CSV):
    """
    Returns (lat, lon, progress_0_1) at the given time in seconds.
    Progress is linear in time based on predicted total time for the route.
    """
    if seconds < 0:
        seconds = 0.0

    pts = _parse_trackpoints(path)
    if not pts:
        raise ValueError("No trackpoints found.")
    cum = _cumulative_distances_m(pts)
    route_len = cum[-1]
    if route_len <= 0:
        la, lo, _ = pts[0]
        return la, lo, 0.0

    total_seconds = predict_total_seconds(path, model_path=model_path, csv_path=csv_path)
    if total_seconds <= 0:
        total_seconds = 1.0

    progress = max(0.0, min(1.0, seconds / total_seconds))
    target_dist = progress * route_len

    idx = next((i for i, d in enumerate(cum) if d >= target_dist), len(cum) - 1)

    if idx == 0:
        la, lo, _ = pts[0]
        return la, lo, progress
    if idx >= len(cum):
        la, lo, _ = pts[-1]
        return la, lo, 1.0

    d0, d1 = cum[idx - 1], cum[idx]
    seg_len = max(1e-9, d1 - d0)
    frac = (target_dist - d0) / seg_len

    (la0, lo0, _e0) = pts[idx - 1]
    (la1, lo1, _e1) = pts[idx]
    lat = _lerp(la0, la1, frac)
    lon = _lerp(lo0, lo1, frac)
    return lat, lon, progress


# ---------- CLI (JSON output) ----------

if __name__ == '__main__':
    import argparse

    parser = argparse.ArgumentParser(description="Train, predict, or locate position along a TCX/GPX route.")
    sub = parser.add_subparsers(dest='cmd', required=True)

    # train
    p_train = sub.add_parser('train', help='Add activity from TCX/GPX and (re)train the model')
    p_train.add_argument('activity_file', help='Path to TCX or GPX file')

    # predict total time
    p_predict = sub.add_parser('predict', help='Predict total time (seconds) for a route')
    p_predict.add_argument('activity_file', help='Path to TCX or GPX file')

    # where (time in seconds)
    p_where = sub.add_parser('where', help='Coordinate after N seconds on the given route')
    p_where.add_argument('activity_file', help='Path to TCX or GPX file')
    p_where.add_argument('seconds', type=float, help='Elapsed time in seconds')

    args = parser.parse_args()

    try:
        if args.cmd == 'train':
            feats = add_activity(args.activity_file)
            try:
                model = train_model()
                n = pd.read_csv(DATA_CSV).shape[0]
                print(json.dumps({
                    "ok": True,
                    "rows": int(n),
                    "last_run": feats
                }))
            except ValueError as e:
                n = pd.read_csv(DATA_CSV).shape[0]
                print(json.dumps({
                    "ok": False,
                    "rows": int(n),
                    "error": str(e),
                    "last_run": feats
                }))

        elif args.cmd == 'predict':
            eta = predict_total_seconds(args.activity_file)
            print(json.dumps({
                "predicted_seconds": float(eta)
            }))

        elif args.cmd == 'where':
            lat, lon, progress = coordinate_at_seconds(args.activity_file, args.seconds)
            print(json.dumps({
                "seconds": float(args.seconds),
                "lat": float(lat),
                "lon": float(lon),
                "progress": float(progress)
            }))

    except Exception as ex:
        # If something goes wrong, print JSON error so C# can log/handle
        print(json.dumps({
            "error": str(ex)
        }))
        raise
