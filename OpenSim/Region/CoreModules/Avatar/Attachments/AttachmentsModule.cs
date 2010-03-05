/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System.Reflection;
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;

namespace OpenSim.Region.CoreModules.Avatar.Attachments
{        
    public class AttachmentsModule : IAttachmentsModule, IRegionModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        protected Scene m_scene = null;

        public void Initialise(Scene scene, IConfigSource source)
        {
            scene.RegisterModuleInterface<IAttachmentsModule>(this);
            m_scene = scene;
        }

        public void PostInitialise()
        {
        }

        public void Close()
        {
        }

        public string Name
        {
            get { return "Attachments Module"; }
        }

        public bool IsSharedModule
        {
            get { return false; }
        }

        public bool AttachObject(
            IClientAPI remoteClient, uint objectLocalID, uint AttachmentPt, Quaternion rot, Vector3 attachPos, bool silent)
        {
            SceneObjectGroup group = m_scene.GetGroupByPrim(objectLocalID);
            if (group != null)
            {
                if (m_scene.Permissions.CanTakeObject(group.UUID, remoteClient.AgentId))
                {
                    // If the attachment point isn't the same as the one previously used
                    // set it's offset position = 0 so that it appears on the attachment point
                    // and not in a weird location somewhere unknown.
                    if (AttachmentPt != 0 && AttachmentPt != (uint)group.GetAttachmentPoint())
                    {
                        attachPos = Vector3.Zero;
                    }

                    // AttachmentPt 0 means the client chose to 'wear' the attachment.
                    if (AttachmentPt == 0)
                    {
                        // Check object for stored attachment point
                        AttachmentPt = (uint)group.GetAttachmentPoint();
                    }

                    // if we still didn't find a suitable attachment point.......
                    if (AttachmentPt == 0)
                    {
                        // Stick it on left hand with Zero Offset from the attachment point.
                        AttachmentPt = (uint)AttachmentPoint.LeftHand;
                        attachPos = Vector3.Zero;
                    }

                    group.SetAttachmentPoint((byte)AttachmentPt);
                    group.AbsolutePosition = attachPos;

                    // Saves and gets itemID
                    UUID itemId;

                    if (group.GetFromItemID() == UUID.Zero)
                    {
                        m_scene.attachObjectAssetStore(remoteClient, group, remoteClient.AgentId, out itemId);
                    }
                    else
                    {
                        itemId = group.GetFromItemID();
                    }

                    m_scene.AttachObject(remoteClient, AttachmentPt, itemId, group);

                    group.AttachToAgent(remoteClient.AgentId, AttachmentPt, attachPos, silent);
                    
                    // In case it is later dropped again, don't let
                    // it get cleaned up
                    group.RootPart.RemFlag(PrimFlags.TemporaryOnRez);
                    group.HasGroupChanged = false;
                }
                else
                {
                    remoteClient.SendAgentAlertMessage(
                        "You don't have sufficient permissions to attach this object", false);
                    
                    return false;
                }
            }
            else
            {
                m_log.DebugFormat("[SCENE GRAPH]: AttachObject found no such scene object {0}", objectLocalID);
                return false;
            }

            return true;
        }        
    }
}