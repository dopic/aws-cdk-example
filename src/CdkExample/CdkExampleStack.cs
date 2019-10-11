using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ElasticLoadBalancingV2;
using Amazon.CDK.AWS.ElasticLoadBalancingV2.Targets;
using Amazon.CDK.AWS.SSM;
using Microsoft.Extensions.Options;

namespace CdkExample
{
    public class CdkExampleStack : Stack
    {
        public CdkExampleStack(Construct parent, string id, IStackProps props) : base(parent, id, props)
        {
            var vpc = new Vpc(this, "MainVPC", new VpcProps
            {
                Cidr = "192.168.0.0/16"
            });
            
            var loadBalancer = new ApplicationLoadBalancer(this, "PublicALB", new ApplicationLoadBalancerProps
            {
                InternetFacing = true,
                Vpc = vpc
            });

            var listener = loadBalancer.AddListener("MyListener", new ApplicationListenerProps
            {
                Port = 80
            });

            var userData = UserData.ForLinux(new LinuxUserDataOptions
            {
                Shebang = "#!/bin/bash"
            });

            userData.AddCommands(
                "yum update -y",
                "yum install httpd -y",
                "echo \"Hello World\" >> /var/www/html/index.html",
                "service httpd start",
                "chkconfig httpd on");
            
            
            var ec2SG =  new SecurityGroup(this, "Ec2SecurityGroup", new SecurityGroupProps
            {
              Vpc  = vpc,
              SecurityGroupName = "Ec2SG"
            });
            
            ec2SG.Connections.AllowFrom(loadBalancer, Port.Tcp(80), "FROM ALB");
            
            var instanceIds = new List<string>();
            for(var ix = 0; ix < vpc.PrivateSubnets.Length;ix++)
            {
                var instance = new Instance_(this, $"Instance-{ix}", new InstanceProps
                {
                    InstanceType = InstanceType.Of(InstanceClass.BURSTABLE3, InstanceSize.MICRO),
                    VpcSubnets = new SubnetSelection()
                    {
                        SubnetType = SubnetType.PRIVATE
                    },
                    AvailabilityZone = vpc.PrivateSubnets[ix].AvailabilityZone,
                    Vpc = vpc,
                    MachineImage = new AmazonLinuxImage(),
                    UserData = userData,
                    KeyName = "test-cdk",
                    SecurityGroup = ec2SG
                } );
                
                instanceIds.Add(instance.InstanceId);
            }

            listener.AddTargets("Targets", new AddApplicationTargetsProps
            {
                Port = 80,
                Targets = instanceIds.Select(i => new InstanceIdTarget(i, 80)).ToArray()
            });
        }
    }
}
