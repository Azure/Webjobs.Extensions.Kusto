import io.gatling.recorder.GatlingRecorder;
import io.gatling.recorder.config.RecorderPropertiesBuilder;
import scala.Option;

public class Recorder {
    public static void main(String[] args) {
        RecorderPropertiesBuilder props = new RecorderPropertiesBuilder()
                .simulationsFolder(IDEPathHelper.mavenSourcesDirectory.toString())
                .resourcesFolder(IDEPathHelper.mavenResourcesDirectory.toString())
                .simulationPackage("com.microsoft.azure.kusto.functions").simulationFormatJava();

        GatlingRecorder.fromMap(props.build(), Option.apply(IDEPathHelper.recorderConfigFile));
    }
}
