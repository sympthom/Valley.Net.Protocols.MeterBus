using System;
using System.Collections.Generic;
using System.Text;

namespace Valley.Net.Protocols.MeterBus.EN13757_2
{

    public class Manufacturer
    {
        public static string Parse(ushort value)
        {
            char[] chr = new char[3];

            for (int i = chr.Length - 1; i >= 0; i--)
            {
                chr[i] = Convert.ToChar((value % 32) + 64);
                value = (ushort)((value - (value % 32)) / 32);
            }

            return new string(chr);
        }


        public enum Manuf : ushort
        {
            ABB = 0x0442, //ABB AB, P.O. Box 1005, SE-61129 Nyköping, Nyköping,Sweden
            ACE = 0x0465, //Actaris (Elektrizität)
            ACG = 0x0467, //Actaris (Gas)
            ACW = 0x0477, //Actaris (Wasser und Wärme)
            AEG = 0x04A7, //AEG
            AEL = 0x04AC, //Kohler, Türkei
            AEM = 0x04AD, //S.C. AEM S.A. Romania
            AMP = 0x05B0, //Ampy Automation Digilog Ltd
            AMT = 0x05B4, //Aquametro
            APS = 0x0613, //Apsis Kontrol Sistemleri, Türkei
            BEC = 0x08A3, //Berg Energiekontrollsysteme GmbH
            BER = 0x08B2, //Bernina Electronic AG
            BSE = 0x0A65, //Basari Elektronik A.S., Türkei
            BST = 0x0A74, //BESTAS Elektronik Optik, Türkei
            CBI = 0x0C49, //Circuit Breaker Industries, Südafrika
            CLO = 0x0D8F, //Clorius Raab Karcher Energie Service A/S
            CON = 0x0DEE, //Conlog
            CZM = 0x0F4D, //Cazzaniga S.p.A.
            DAN = 0x102E, //Danubia
            DFS = 0x10D3, //Danfoss A/S
            DME = 0x11A5, //DIEHL Metering, Industriestrasse 13, 91522 Ansbach, Germany
            DZG = 0x1347, //Deutsche Zählergesellschaft
            EDM = 0x148D, //EDMI Pty.Ltd.
            EFE = 0x14C5, //Engelmann Sensor GmbH
            EKT = 0x1574, //PA KVANT J.S., Russland
            ELM = 0x158D, //Elektromed Elektronik Ltd, Türkei
            ELS = 0x1593, //ELSTER Produktion GmbH
            EMH = 0x15A8, //EMH Elektrizitätszähler GmbH & CO KG
            EMU = 0x15B5, //EMU Elektronik AG
            EMO = 0x15AF, //Enermet
            END = 0x15C4, //ENDYS GmbH
            ENP = 0x15D0, //Kiev Polytechnical Scientific Research
            ENT = 0x15D4, //ENTES Elektronik, Türkei
            ERL = 0x164C, //Erelsan Elektrik ve Elektronik, Türkei
            ESM = 0x166D, //Starion Elektrik ve Elektronik, Türkei
            EUR = 0x16B2, //Eurometers Ltd
            EWT = 0x16F4, //Elin Wasserwerkstechnik
            FED = 0x18A4, //Federal Elektrik, Türkei
            FML = 0x19AC, //Siemens Measurements Ltd.( Formerly FML Ltd.)
            GBJ = 0x1C4A, //Grundfoss A/S
            GEC = 0x1CA3, //GEC Meters Ltd.
            GSP = 0x1E70, //Ingenieurbuero Gasperowicz
            GWF = 0x1EE6, //Gas- u. Wassermessfabrik Luzern
            HEG = 0x20A7, //Hamburger Elektronik Gesellschaft
            HEL = 0x20AC, //Heliowatt
            HTC = 0x2283, //Horstmann Timers and Controls Ltd.
            HYD = 0x2324, //Hydrometer GmbH
            ICM = 0x246D, //Intracom, Griechenland
            IDE = 0x2485, //IMIT S.p.A.
            INV = 0x25D6, //Invensys Metering Systems AG
            ISK = 0x266B, //Iskraemeco, Slovenia
            IST = 0x2674, //ista Deutschland (bis 2005 Viterra Energy Services)
            ITR = 0x2692, //Itron
            IWK = 0x26EB, //IWK Regler und Kompensatoren GmbH
            KAM = 0x2C2D, //Kamstrup Energie A/S
            KHL = 0x2D0C, //Kohler, Türkei
            KKE = 0x2D65, //KK-Electronic A/S
            KNX = 0x2DD8, //KONNEX-based users (Siemens Regensburg)
            KRO = 0x2E4F, //Kromschröder
            KST = 0x2E74, //Kundo SystemTechnik GmbH
            LEM = 0x30AD, //LEM HEME Ltd., UK
            LGB = 0x3000, //Landis & Gyr Energy Management (UK) Ltd.
            LGD = 0x30E2, //Landis & Gyr Deutschland
            LGZ = 0x30FA, //Landis & Gyr Zug
            LHA = 0x3101, //Atlantic Meters, Südafrika
            LML = 0x31AC, //LUMEL, Polen
            LSE = 0x3265, //Landis & Staefa electronic
            LSP = 0x3270, //Landis & Staefa production
            LUG = 0x32A7, //Landis & Staefa
            LSZ = 0x327A, //Siemens Building Technologies
            MAD = 0x3424, //Maddalena S.r.I., Italien
            MEI = 0x34A9, //H. Meinecke AG (jetzt Invensys Metering Systems AG)
            MKS = 0x3573, //MAK-SAY Elektrik Elektronik, Türkei
            MNS = 0x35D3, //MANAS Elektronik, Türkei
            MPS = 0x3613, //Multiprocessor Systems Ltd, Bulgarien
            MTC = 0x3683, //Metering Technology Corporation, USA
            NIS = 0x3933, //Nisko Industries Israel
            NMS = 0x39B3, //Nisko Advanced Metering Solutions Israel
            NRM = 0x3A4D, //Norm Elektronik, Türkei
            ONR = 0x3DD2, //ONUR Elektroteknik, Türkei
            PAD = 0x4024, //PadMess GmbH
            PMG = 0x41A7, //Spanner-Pollux GmbH (jetzt Invensys Metering Systems AG)
            PRI = 0x4249, //Polymeters Response International Ltd.
            RAS = 0x4833, //Hydrometer GmbH
            REL = 0x48AC, //Relay GmbH
            RKE = 0x4965, //Raab Karcher ES (jetzt ista Deutschland)
            SAP = 0x4C30, //Sappel
            SCH = 0x4C68, //Schnitzel GmbH
            SEN = 0x4CAE, //Sensus GmbH
            SMC = 0x4DA3, // 
            SME = 0x4DA5, //Siame, Tunesien
            SML = 0x4DAC, //Siemens Measurements Ltd.
            SIE = 0x4D25, //Siemens AG
            SLB = 0x4D82, //Schlumberger Industries Ltd.
            SON = 0x4DEE, //Sontex SA
            SOF = 0x4DE6, //softflow.de GmbH
            SPL = 0x4E0C, //Sappel
            SPX = 0x4E18, //Spanner Pollux GmbH (jetzt Invensys Metering Systems AG)
            SVM = 0x4ECD, //AB Svensk Värmemätning SVM
            TCH = 0x5068, //Techem Service AG
            TIP = 0x5130, //TIP Thüringer Industrie Produkte GmbH
            UAG = 0x5427, //Uher
            UGI = 0x54E9, //United Gas Industries
            VES = 0x58B3, //ista Deutschland (bis 2005 Viterra Energy Services)
            VPI = 0x5A09, //Van Putten Instruments B.V.
            WMO = 0x5DAF, //Westermo Teleindustri AB, Schweden
            YTE = 0x6685, //Yuksek Teknoloji, Türkei
            ZAG = 0x6827, //Zellwerg Uster AG
            ZAP = 0x6830, //Zaptronix
            ZIV = 0x6936, //
        }

    }
}
