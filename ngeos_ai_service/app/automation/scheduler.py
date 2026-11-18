import asyncio
from apscheduler.schedulers.asyncio import AsyncIOScheduler
from app.config import settings
from app.logger import logger
from app.agents.admin_agent import admin_agent
from app.bridge.dotnet_client import dotnet_bridge

scheduler = AsyncIOScheduler()

async def job_donation_monitoring():
    logger.info("scheduler_executing_donation_monitoring")
    try:
        res = await admin_agent.run_autonomous_donation_audit()
        logger.info("scheduler_donation_audit_finished", result=res)
    except Exception as ex:
        logger.error("scheduler_donation_audit_failed", error=str(ex))

async def job_health_monitoring():
    logger.info("scheduler_executing_health_check")
    try:
        health = await dotnet_bridge.get_system_health()
        if health.get("status") in ["Degraded", "Unreachable"]:
            logger.warning("scheduler_health_warning", health=health)
            await dotnet_bridge.broadcast_signalr(
                target_group="Admins",
                event_name="SystemHealthWarning",
                data=health
            )
    except Exception as ex:
        logger.error("scheduler_health_check_failed", error=str(ex))

def start_background_automation():
    if not scheduler.running:
        scheduler.add_job(
            job_donation_monitoring, 
            "interval", 
            minutes=settings.donation_monitor_interval_minutes,
            id="donation_monitor_job",
            replace_existing=True
        )
        scheduler.add_job(
            job_health_monitoring, 
            "interval", 
            minutes=settings.health_check_interval_minutes,
            id="health_monitor_job",
            replace_existing=True
        )
        scheduler.start()
        logger.info("background_automation_scheduler_started")

def stop_background_automation():
    if scheduler.running:
        scheduler.shutdown(wait=False)
        logger.info("background_automation_scheduler_stopped")
