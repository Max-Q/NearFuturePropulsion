﻿/// ElectricEngineThrustLimiter
/// ---------------------------
/// Module that limits the thrust of an engine while in an atmosphere instead of changing ISP
/// 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSPAPIExtensions;

namespace NearFuturePropulsion
{
    class ElectricEngineThrustLimiter:PartModule
    {

        // Thrust
        [KSPField(isPersistant = false)]
        public float minThrust = 0f;
        [KSPField(isPersistant = false)]
        public float minPressure = 1f;


        private FloatCurve ThrustCurve;
        private FloatCurve AtmoCurve;
        private ModuleEnginesFX engine;

        private bool started = false;

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Power Input", guiFormat = "S2", guiUnits = " Ec/s")]
        public float EnergyUsage = 100f;

        private Propellant fuelPropellant;
        private Propellant ecPropellant;

        public override string GetInfo()
        {
            return String.Format("Power Input: {0:F1} Ec/s", Utils.FindPowerUse(engine.maxThrust, engine.atmosphereCurve.Evaluate(0f), fuelPropellant, ecPropellant)) + "\n" +
                  String.Format("Fuel: " + fuelPropellant.name + "\n");

        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            this.moduleName = "Electric Engine";

        }
        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            engine = part.GetComponent<ModuleEnginesFX>();

            foreach (Propellant prop in engine.propellants)
            {

                if (prop.name != "ElectricCharge")
                    fuelPropellant = prop;
                else
                    ecPropellant = prop;
            }

            ThrustCurve = new FloatCurve();
            ThrustCurve.Add(0f, engine.maxThrust);
            ThrustCurve.Add(minPressure, minThrust);

            AtmoCurve = new FloatCurve();
            AtmoCurve.Add(0f, engine.atmosphereCurve.Evaluate(0f));

            float rate = FindFlowRate (engine.maxThrust,engine.atmosphereCurve.Evaluate(0f),fuelPropellant); 

            AtmoCurve.Add(1f,FindIsp(minThrust,rate,fuelPropellant));

            if (state != StartState.Editor)
                started = true;
            else
                started = false;
        }

        // finds the flow rate given thrust, isp and the propellant 
        private float FindFlowRate(float thrust, float isp,Propellant fuelPropellant)
        {
             double fuelDensity = PartResourceLibrary.Instance.GetDefinition(fuelPropellant.name).density;
             double fuelRate = ((thrust * 1000f) / (isp * 9.82d)) / (fuelDensity*1000f);
             return (float)fuelRate;
        }

        private float FindIsp(float thrust, float flowRate, Propellant fuelPropellant)
        {
            double fuelDensity = PartResourceLibrary.Instance.GetDefinition(fuelPropellant.name).density;
            double isp = (((minThrust * 1000f) / ( 9.82d)) / flowRate) / (fuelDensity * 1000f);
            return (float)isp;
        }

        public  void FixedUpdate()
        {
            if (KSPAPIExtensions.PartUtils.IsLoaded(GameSceneFilter.Flight))
            {
                engine.maxThrust = ThrustCurve.Evaluate((float)FlightGlobals.getStaticPressure(vessel.transform.position));
                engine.atmosphereCurve = new FloatCurve();
                engine.atmosphereCurve.Add(0f, AtmoCurve.Evaluate((float)FlightGlobals.getStaticPressure(vessel.transform.position)));

                EnergyUsage = Utils.FindPowerUse(engine.maxThrust, engine.atmosphereCurve.Evaluate(0f), fuelPropellant, ecPropellant);
            }

            if (KSPAPIExtensions.PartUtils.IsLoaded(GameSceneFilter.AnyEditorOrFlight))
            {
                EnergyUsage = Utils.FindPowerUse(engine.maxThrust, engine.atmosphereCurve.Evaluate(0f), ecPropellant, fuelPropellant);
            }
                
            

        }

        

    }
}