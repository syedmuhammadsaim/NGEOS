"""
NGEOS 100% Free Intelligence APIs
Zero cost, no credit-card, production-ready free services:
1. FreeGeoIpService (ip-api.com) - IP Geolocation & Threat Origin
2. FreeCurrencyService (open.er-api.com) - Real-time Foreign Exchange to PKR
3. FreeDisasterWeatherService (open-meteo.com) - Humanitarian Flood & Weather Emergency Tracker
"""

import time
from typing import Dict, Any, Optional
import httpx
from app.logger import logger

class FreeGeoIpService:
    """
    100% Free Geolocation & Threat Origin lookup using ip-api.com (No API Key Required).
    """
    def __init__(self):
        self._cache: Dict[str, Dict[str, Any]] = {}

    async def lookup_ip(self, ip: str) -> Dict[str, Any]:
        if not ip or ip in ["127.0.0.1", "::1", "localhost"]:
            return {
                "country": "Pakistan",
                "countryCode": "PK",
                "city": "Karachi",
                "region": "Sindh",
                "isp": "Local Development",
                "isProxy": False
            }

        if ip in self._cache:
            return self._cache[ip]

        url = f"http://ip-api.com/json/{ip}?fields=status,message,country,countryCode,regionName,city,isp,proxy,hosting"
        try:
            async with httpx.AsyncClient(timeout=4.0) as client:
                res = await client.get(url)
                if res.status_code == 200:
                    data = res.json()
                    if data.get("status") == "success":
                        result = {
                            "country": data.get("country", "Unknown"),
                            "countryCode": data.get("countryCode", ""),
                            "city": data.get("city", ""),
                            "region": data.get("regionName", ""),
                            "isp": data.get("isp", ""),
                            "isProxy": bool(data.get("proxy") or data.get("hosting"))
                        }
                        self._cache[ip] = result
                        return result
        except Exception as ex:
            logger.warning("free_geoip_lookup_error", ip=ip, error=str(ex))

        return {
            "country": "Unknown",
            "countryCode": "",
            "city": "",
            "region": "",
            "isp": "",
            "isProxy": False
        }

class FreeCurrencyService:
    """
    100% Free Open Exchange Rates API (No API Key Required).
    Fetches live USD, EUR, GBP, AED, SAR conversion to PKR with in-memory caching.
    """
    def __init__(self):
        self._rates_cache: Dict[str, float] = {
            "USD": 278.0,
            "EUR": 298.0,
            "GBP": 352.0,
            "AED": 75.6,
            "SAR": 74.1,
            "PKR": 1.0
        }
        self._last_fetch: float = 0.0

    async def get_live_rates(self) -> Dict[str, float]:
        now = time.time()
        # Refresh cache every 6 hours
        if now - self._last_fetch < 21600 and self._rates_cache:
            return self._rates_cache

        url = "https://open.er-api.com/v6/latest/USD"
        try:
            async with httpx.AsyncClient(timeout=5.0) as client:
                res = await client.get(url)
                if res.status_code == 200:
                    data = res.json()
                    rates = data.get("rates", {})
                    pkr_per_usd = rates.get("PKR", 278.0)

                    self._rates_cache = {
                        "USD": round(pkr_per_usd, 2),
                        "EUR": round(pkr_per_usd / rates.get("EUR", 0.92), 2) if rates.get("EUR") else 300.0,
                        "GBP": round(pkr_per_usd / rates.get("GBP", 0.79), 2) if rates.get("GBP") else 350.0,
                        "AED": round(pkr_per_usd / rates.get("AED", 3.67), 2) if rates.get("AED") else 75.0,
                        "SAR": round(pkr_per_usd / rates.get("SAR", 3.75), 2) if rates.get("SAR") else 74.0,
                        "PKR": 1.0
                    }
                    self._last_fetch = now
                    logger.info("currency_rates_refreshed", pkr_rate=pkr_per_usd)
        except Exception as ex:
            logger.warning("free_currency_fetch_failed", error=str(ex))

        return self._rates_cache

    async def convert_to_pkr(self, amount: float, currency: str) -> float:
        rates = await self.get_live_rates()
        curr = currency.upper().strip()
        rate = rates.get(curr, 1.0)
        return round(amount * rate, 2)

class FreeDisasterWeatherService:
    """
    100% Free Humanitarian Weather & Flood Risk Tracker using Open-Meteo API (No Key Required).
    Monitors high-vulnerability regions (Sindh, Balochistan, South Punjab, KPK) for crisis alerts.
    """
    REGIONS = {
        "Karachi / Coastal Sindh": {"lat": 24.86, "lon": 67.00},
        "Sukkur / Northern Sindh": {"lat": 27.70, "lon": 68.85},
        "Quetta / Balochistan": {"lat": 30.17, "lon": 66.97},
        "Peshawar / KPK": {"lat": 34.01, "lon": 71.52},
        "Multan / South Punjab": {"lat": 30.15, "lon": 71.52}
    }

    async def get_humanitarian_weather_alerts(self) -> Dict[str, Any]:
        alerts = []
        try:
            async with httpx.AsyncClient(timeout=5.0) as client:
                for region_name, coords in self.REGIONS.items():
                    url = f"https://api.open-meteo.com/v1/forecast?latitude={coords['lat']}&longitude={coords['lon']}&current_weather=true"
                    res = await client.get(url)
                    if res.status_code == 200:
                        data = res.json()
                        curr = data.get("current_weather", {})
                        temp = curr.get("temperature", 25.0)
                        wind = curr.get("windspeed", 10.0)
                        weather_code = curr.get("weathercode", 0)

                        # High wind or severe weather code indicator
                        if wind > 45 or weather_code in [95, 96, 99]: # Thunderstorms / High storm
                            alerts.append({
                                "region": region_name,
                                "temperature": temp,
                                "windspeed": wind,
                                "severity": "High",
                                "alert": "Severe storm conditions reported - Emergency Flood Relief readiness recommended."
                            })
        except Exception as ex:
            logger.warning("disaster_weather_check_failed", error=str(ex))

        return {
            "monitoredRegionsCount": len(self.REGIONS),
            "activeEmergencyAlerts": alerts,
            "crisisAlertCount": len(alerts)
        }

free_geoip_service = FreeGeoIpService()
free_currency_service = FreeCurrencyService()
free_disaster_service = FreeDisasterWeatherService()
