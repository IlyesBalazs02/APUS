import os
import pickle
import pandas as pd
import xml.etree.ElementTree as ET
import json
from datetime import datetime
from math import radians, sin, cos, sqrt, atan2
from sklearn.linear_model import HuberRegressor

DATA_CSV = "Running.csv"
MODEL_FILE = "Running.pkl"



def _haversine_m(lat1, lon1, lat2, lon2):
    R = 6371000.0
    dlat = radians(lat2 - lat1)
    dlon = radians(lon2 - lon1)
    a = sin(dlat / 2) ** 2 + cos(radians(lat1)) * cos(radians(lat2)) * sin(dlon / 2) ** 2
    c = 2 * atan2(sqrt(a), sqrt(1 - a))
    return R * c


def _sanitize_time_for_prediction(distance_km, times):
    """
    Decide whether timestamps look usable.
    If they appear synthetic (e.g., 1s increments for long distance),
    or missing, return None so we ignore total_time_s for this file.
    """
    if not times or len(times) < 2:
        return None

    try:
        t0 = datetime.fromisoformat(times[0].replace('Z', '+00:00'))
        t1 = datetime.fromisoformat(times[-1].replace('Z', '+00:00'))
    except Exception:
        return None

    total = (t1 - t0).total_seconds()


    # distance > 1 km but total < 60 sec -> impossible
    if distance_km > 1.0 and total < 60:
        return None

    # distance > 3 km but total < 5 minutes -> highly improbable
    if distance_km > 3.0 and total < 300:
        return None

    # non-positive duration -> invalid
    if total <= 0:
        return None

    return total


def _compute_features_from_points(points, times):
    """
    points: list[(lat, lon, ele_or_None)]
    times : list[str ISO timestamps] – may be empty or synthetic.
    """

    if not points:
        return {
            "distance_km": 0.0,
            "elevation_gain_m": 0.0,
            "slope": 0.0,
            "total_time_s": None,
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

    total_time_s = _sanitize_time_for_prediction(distance_km, times)

    return {
        "distance_km": distance_km,
        "elevation_gain_m": total_gain,
        "slope": slope,
        "total_time_s": total_time_s,
    }


# ---------- TCX parsing ----------

def parse_tcx(file_path: str) -> dict:
    tree = ET.parse(file_path)
    root = tree.getroot()
    ns = {"tcx": "http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2"}

    points = []
    times = []

    for tp in root.findall(".//tcx:Trackpoint", ns):
        time_elem = tp.find("tcx:Time", ns)
        if time_elem is not None and time_elem.text:
            times.append(time_elem.text)

        ele_elem = tp.find("tcx:AltitudeMeters", ns)
        ele = float(ele_elem.text) if (ele_elem is not None and ele_elem.text) else None

        lat_elem = tp.find(".//tcx:LatitudeDegrees", ns)
        lon_elem = tp.find(".//tcx:LongitudeDegrees", ns)
        if lat_elem is None or lon_elem is None or not lat_elem.text or not lon_elem.text:
            continue
        lat = float(lat_elem.text)
        lon = float(lon_elem.text)

        points.append((lat, lon, ele))

    return _compute_features_from_points(points, times)


def _parse_trackpoints_tcx(file_path: str):
    tree = ET.parse(file_path)
    root = tree.getroot()
    ns = {"tcx": "http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2"}
    pts = []
    for tp in root.findall(".//tcx:Trackpoint", ns):
        lat_e = tp.find(".//tcx:LatitudeDegrees", ns)
        lon_e = tp.find(".//tcx:LongitudeDegrees", ns)
        ele_e = tp.find("tcx:AltitudeMeters", ns)
        if lat_e is None or lon_e is None or not lat_e.text or not lon_e.text:
            continue
        lat = float(lat_e.text)
        lon = float(lon_e.text)
        ele = float(ele_e.text) if (ele_e is not None and ele_e.text) else None
        pts.append((lat, lon, ele))
    return pts


# ---------- GPX parsing ----------

def _discover_gpx_ns(root):
    if root.tag.startswith("{"):
        uri = root.tag[1:].split("}")[0]
        return {"gpx": uri}
    return {}


def parse_gpx(file_path: str) -> dict:
    tree = ET.parse(file_path)
    root = tree.getroot()
    ns = _discover_gpx_ns(root)

    if ns:
        trkpts = root.findall(".//gpx:trkpt", ns)
    else:
        trkpts = root.findall(".//trkpt")

    points = []
    times = []

    for tp in trkpts:
        lat_attr = tp.get("lat")
        lon_attr = tp.get("lon")
        if not lat_attr or not lon_attr:
            continue
        lat = float(lat_attr)
        lon = float(lon_attr)

        if ns:
            ele_elem = tp.find("gpx:ele", ns)
            time_elem = tp.find("gpx:time", ns)
        else:
            ele_elem = tp.find("ele")
            time_elem = tp.find("time")

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
        trkpts = root.findall(".//gpx:trkpt", ns)
    else:
        trkpts = root.findall(".//trkpt")

    pts = []
    for tp in trkpts:
        lat_attr = tp.get("lat")
        lon_attr = tp.get("lon")
        if not lat_attr or not lon_attr:
            continue
        lat = float(lat_attr)
        lon = float(lon_attr)

        if ns:
            ele_elem = tp.find("gpx:ele", ns)
        else:
            ele_elem = tp.find("ele")

        ele = float(ele_elem.text) if (ele_elem is not None and ele_elem.text) else None
        pts.append((lat, lon, ele))
    return pts


def parse_activity(file_path: str) -> dict:
    ext = os.path.splitext(file_path)[1].lower()
    if ext == ".tcx":
        return parse_tcx(file_path)
    elif ext == ".gpx":
        return parse_gpx(file_path)
    else:
        raise ValueError(f"Unsupported file type: {file_path}. Use .tcx or .gpx")


def _parse_trackpoints(file_path: str):
    ext = os.path.splitext(file_path)[1].lower()
    if ext == ".tcx":
        return _parse_trackpoints_tcx(file_path)
    elif ext == ".gpx":
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



def ensure_data_store(csv_path: str = DATA_CSV):
    if not os.path.exists(csv_path):
        pd.DataFrame(columns=["distance_km", "elevation_gain_m", "slope", "total_time_s"]).to_csv(csv_path, index=False)


def add_activity(file_path: str, csv_path: str = DATA_CSV) -> dict:
    """
    Add a real recorded activity (TCX/GPX *with real timestamps*) to the CSV.
    If total_time_s is None (e.g., planned route or synthetic timestamps), we skip it.
    """
    feats = parse_activity(file_path)
    if feats["total_time_s"] is None:
        raise ValueError("Could not extract reliable total_time_s from file (likely planned route or invalid timestamps).")

    ensure_data_store(csv_path)
    df = pd.read_csv(csv_path)
    df = pd.concat([df, pd.DataFrame([feats])], ignore_index=True) if not df.empty else pd.DataFrame([feats])
    df.to_csv(csv_path, index=False)
    return feats


def train_model(csv_path: str = DATA_CSV, model_path: str = MODEL_FILE) -> HuberRegressor:
    """
    Train a robust regression model (HuberRegressor) on cleaned data:
    - positive distance
    - positive total_time
    - pace in [150, 900] s/km  (~2:30 to 15:00 min/km)
    """
    df = pd.read_csv(csv_path)
    if df.shape[0] < 2:
        raise ValueError("Need at least two data points to train a regression model.")

    df = df[(df["distance_km"] > 0) & (df["total_time_s"] > 0)]
    if df.empty:
        raise ValueError("No valid rows (positive distance & time) to train model.")

    pace = df["total_time_s"] / df["distance_km"]
    MIN_PACE = 150.0  # 2:30
    MAX_PACE = 900.0  # 15:00
    df = df[(pace > MIN_PACE) & (pace < MAX_PACE)]
    if df.shape[0] < 2:
        raise ValueError("Not enough valid data points after filtering unrealistic paces.")

    X = df[["distance_km", "elevation_gain_m", "slope"]]
    y = df["total_time_s"]

    model = HuberRegressor()
    model.fit(X, y)

    with open(model_path, "wb") as f:
        pickle.dump(model, f)

    return model


def load_model(model_path: str = MODEL_FILE) -> HuberRegressor:
    if not os.path.exists(model_path):
        raise FileNotFoundError(f"Model file not found: {model_path}")
    with open(model_path, "rb") as f:
        return pickle.load(f)


def predict_total_seconds(file_path: str,
                          model_path: str = MODEL_FILE,
                          csv_path: str = DATA_CSV) -> float:
    """
    Predict total time for a TCX/GPX file.
    Robustness:
    - use HuberRegressor
    - enforce realistic pace bounds
    - if prediction is insane, fall back to median pace from history
    """
    feats = parse_activity(file_path)
    dist_km = feats["distance_km"]

    if dist_km <= 0:
        raise RuntimeError("Non-positive distance in activity.")

    # realistic running pace limits
    MIN_PACE = 150.0  # 2:30 min/km
    MAX_PACE = 900.0  # 15:00 min/km

    min_time = dist_km * MIN_PACE
    max_time = dist_km * MAX_PACE

    try:
        model = load_model(model_path)
        X = [[feats["distance_km"], feats["elevation_gain_m"], feats["slope"]]]
        raw_pred = float(model.predict(X)[0])

        if raw_pred < min_time or raw_pred > max_time:
            df = pd.read_csv(csv_path)
            if df.shape[0] == 0:
                default_pace = 360.0
                return dist_km * default_pace

            pace_series = df["total_time_s"] / df["distance_km"]
            median_pace = float(pace_series.median())
            return median_pace * dist_km

        return raw_pred

    except (FileNotFoundError, ValueError):
        df = pd.read_csv(csv_path)
        if df.shape[0] == 0:
            default_pace = 360.0
            return dist_km * default_pace

        pace_series = df["total_time_s"] / df["distance_km"]
        median_pace = float(pace_series.median())
        return median_pace * dist_km


def coordinate_at_seconds(path: str,
                          seconds: float,
                          model_path: str = MODEL_FILE,
                          csv_path: str = DATA_CSV):
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


# ---------- (JSON output) ----------

if __name__ == "__main__":
    import argparse

    parser = argparse.ArgumentParser(description="Train, predict, or locate position along a TCX/GPX route.")
    sub = parser.add_subparsers(dest="cmd", required=True)

    # train
    p_train = sub.add_parser("train", help="Add activity from TCX/GPX and (re)train the model")
    p_train.add_argument("activity_file", help="Path to TCX or GPX file")

    # predict total time
    p_predict = sub.add_parser("predict", help="Predict total time (seconds) for a route")
    p_predict.add_argument("activity_file", help="Path to TCX or GPX file")

    # where (time in seconds)
    p_where = sub.add_parser("where", help="Coordinate after N seconds on the given route")
    p_where.add_argument("activity_file", help="Path to TCX or GPX file")
    p_where.add_argument("seconds", type=float, help="Elapsed time in seconds")

    args = parser.parse_args()

    try:
        if args.cmd == "train":
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

        elif args.cmd == "predict":
            eta = predict_total_seconds(args.activity_file)
            print(json.dumps({
                "predicted_seconds": float(eta)
            }))

        elif args.cmd == "where":
            lat, lon, progress = coordinate_at_seconds(args.activity_file, args.seconds)
            print(json.dumps({
                "seconds": float(args.seconds),
                "lat": float(lat),
                "lon": float(lon),
                "progress": float(progress)
            }))

    except Exception as ex:
        print(json.dumps({
            "error": str(ex)
        }))
        raise
