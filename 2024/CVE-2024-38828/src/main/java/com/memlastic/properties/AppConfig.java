package com.memlastic.properties;

import lombok.Data;
import org.springframework.boot.context.properties.ConfigurationProperties;

import javax.annotation.PostConstruct;

/**
 * A configuration class that sets up file upload limits in mb
 */
@Data
@ConfigurationProperties(prefix = "app")
public class AppConfig {
    private Integer limit = 1;

    @PostConstruct
    void postConstructor() {
        limit = limit * 1024;
        limit = limit * 1024;
    }
}
